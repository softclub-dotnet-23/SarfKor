# Sarfkor — Frontend

React 19 + TypeScript + Vite + Tailwind CSS v4 + Framer Motion.

- Public landing page: scroll-reveal sections, a mouse-tilt iPhone mockup, an interactive store map, a testimonials carousel, an accordion FAQ, and a light/dark theme toggle.
- `/login` — real registration/login against the backend (ASP.NET Identity + JWT).
- `/admin` — StorePartner panel (dashboard, POS, inventory, staff/shifts, reports, settings), wired to the real backend wherever an endpoint exists. Where the backend has no supporting endpoint yet (store profile editing, product catalog/search, staff roster, notifications, password change), the UI says so explicitly instead of faking it — see comments in `src/lib/api/*` and the relevant page components.

## Development

```bash
npm install
npm run dev      # start dev server
npm run build     # type-check + production build
npm run lint      # oxlint
```

## Connecting to the backend

Copy `.env.example` to `.env.local` and set `VITE_API_BASE_URL` if the backend isn't running on the default `http://localhost:5135`.

For the admin panel and login to work, the backend (`Backend/src/WebApi`) must be running with:
- `Cors:AllowedOrigins` including this dev server's origin (`http://localhost:5173` by default) — otherwise every request fails as a browser CORS error regardless of frontend code.
- A configured Postgres connection string and JWT signing key (see the backend's own config/secrets setup).

Without a running backend, `/login` and everything under `/admin` will show connection errors — the public landing page still works standalone.
