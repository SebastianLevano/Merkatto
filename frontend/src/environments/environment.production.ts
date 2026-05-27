// Production: SPA and API share the same origin behind the reverse proxy (Caddy routes /api/*).
export const environment = {
  production: true,
  apiUrl: '/api/v1'
};
