export const environment = {
  production: true,
  // URL base del BFF. En despliegue se sirve tras nginx en el mismo origen, por eso queda vacío
  // y las llamadas usan rutas relativas (/api, /hubs).
  bffBaseUrl: ''
};
