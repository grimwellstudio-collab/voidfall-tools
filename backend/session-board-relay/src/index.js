// Session Board relay — tiny presence/event store for the Unity editor dashboard.
// One Durable Object per room. Auth: ?room=<name>&key=<TEAM_KEY secret>.

const CLAIM_TTL_TICKS = 288e9; // 8 hours, in .NET ticks (100ns units)

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
      return Response.json({
        presence: [...map.values()],
        events: events.slice(-50),
        claims: Object.values(claims),
      });
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
