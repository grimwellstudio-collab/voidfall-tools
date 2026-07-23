// Session Board relay — tiny presence/event store for the Unity editor dashboard.
// One Durable Object per room. Auth: ?room=<name>&key=<TEAM_KEY secret>.

const CLAIM_TTL_TICKS = 288e9; // 8 hours, in .NET ticks (100ns units)
const PRESENCE_TTL_TICKS = 6048e9; // 7 days, in .NET ticks (100ns units)
const HISTORY_MAX_DAYS = 31;

export class Room {
  constructor(state) {
    this.state = state;
  }

  async fetch(request) {
    const url = new URL(request.url);

    if (request.method === "POST" && url.pathname === "/presence") {
      const body = await request.json();
      if (!body || !body.userName) return new Response("bad request", { status: 400 });
      await this.state.storage.put("p:" + body.userName, body);
      if (typeof body.dayStamp === "string" && body.dayStamp) {
        await this.state.storage.put("d:" + body.dayStamp + ":" + body.userName, {
          date: body.dayStamp,
          userName: body.userName,
          minutes: body.dayMinutes | 0,
          saves: body.daySaves | 0,
          playtests: body.dayPlaytests | 0,
          scriptEdits: body.dayScriptEdits | 0,
          scriptLines: body.dayScriptLines | 0,
        });
      }
      return Response.json({ ok: true });
    }

    if (request.method === "POST" && url.pathname === "/event") {
      const body = await request.json();
      if (!body || !body.userName || !body.message) return new Response("bad request", { status: 400 });
      const events = (await this.state.storage.get("events")) || [];
      events.push(body);
      while (events.length > 200) events.shift();
      await this.state.storage.put("events", events);
      return Response.json({ ok: true });
    }

    if (request.method === "POST" && url.pathname === "/claim") {
      const body = await request.json();
      if (!body || !body.item || !body.userName || !body.utcTicks)
        return new Response("bad request", { status: 400 });
      const claims = (await this.state.storage.get("claims")) || {};
      claims[body.item] = { item: body.item, userName: body.userName, utcTicks: body.utcTicks };
      await this.state.storage.put("claims", claims);
      return Response.json({ ok: true });
    }

    if (request.method === "POST" && url.pathname === "/release") {
      const body = await request.json();
      if (!body || !body.item || !body.userName) return new Response("bad request", { status: 400 });
      const claims = (await this.state.storage.get("claims")) || {};
      if (claims[body.item] && claims[body.item].userName === body.userName) {
        delete claims[body.item];
        await this.state.storage.put("claims", claims);
      }
      return Response.json({ ok: true });
    }

    // wipe the whole room — presence, events, claims, history
    if (request.method === "POST" && url.pathname === "/reset") {
      await this.state.storage.deleteAll();
      return Response.json({ ok: true });
    }

    if (request.method === "GET" && url.pathname === "/state") {
      const map = await this.state.storage.list({ prefix: "p:" });
      const events = (await this.state.storage.get("events")) || [];
      const claims = (await this.state.storage.get("claims")) || {};
      const now = Date.now() * 10000 + 621355968000000000; // approx .NET ticks for utcnow
      let expired = false;
      for (const key of Object.keys(claims)) {
        if (now - claims[key].utcTicks > CLAIM_TTL_TICKS) {
          delete claims[key];
          expired = true;
        }
      }
      if (expired) await this.state.storage.put("claims", claims);
      const staleKeys = [];
      for (const [key, entry] of map) {
        if (now - entry.heartbeatUtcTicks > PRESENCE_TTL_TICKS) staleKeys.push(key);
      }
      for (const key of staleKeys) {
        map.delete(key);
        await this.state.storage.delete(key);
      }
      return Response.json({
        presence: [...map.values()],
        events: events.slice(-50),
        claims: Object.values(claims),
      });
    }

    if (request.method === "GET" && url.pathname === "/history") {
      let days = parseInt(url.searchParams.get("days"), 10);
      if (!Number.isFinite(days)) days = 14;
      days = Math.max(1, Math.min(31, days));
      const cutoff = new Date(Date.now() - days * 86400000).toISOString().slice(0, 10);
      const maxCutoff = new Date(Date.now() - HISTORY_MAX_DAYS * 86400000).toISOString().slice(0, 10);
      const map = await this.state.storage.list({ prefix: "d:" });
      const history = [];
      const oldKeys = [];
      for (const [key, entry] of map) {
        if (entry.date < maxCutoff) oldKeys.push(key);
        if (entry.date >= cutoff) history.push(entry);
      }
      for (const key of oldKeys) await this.state.storage.delete(key);
      return Response.json({ history });
    }

    return new Response("not found", { status: 404 });
  }
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const room = url.searchParams.get("room");
    const key = url.searchParams.get("key");
    if (!room || !env.TEAM_KEY || key !== env.TEAM_KEY)
      return new Response("unauthorized", { status: 401 });
    const id = env.ROOMS.idFromName(room);
    return env.ROOMS.get(id).fetch(request);
  },
};
