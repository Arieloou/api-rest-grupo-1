# API de Gestión de Pedidos

API REST desarrollada con ASP.NET Core 10 para administrar clientes y sus pedidos. El contrato formal se encuentra en [`openapi.yaml`](openapi.yaml) y la colección ejecutable en [`postman/API.postman_collection.json`](postman/API.postman_collection.json).

## Problema de integración

Un canal de ventas necesita registrar y mantener sus clientes, además de crear y administrar los pedidos que realizan. La API desacopla ese canal del sistema encargado de validar clientes e inventario, calcular totales y conservar la información, ofreciendo un contrato HTTP uniforme para la integración.

## Sistemas involucrados

| Rol | Sistema | Responsabilidad |
|---|---|---|
| Consumidor | Canal de ventas (web, móvil o punto de venta) | Envía pedidos y consulta sus resultados. |
| Proveedor | Servicio de Gestión de Pedidos | Valida inventario, calcula totales y expone los pedidos. |

## Recursos principales del dominio

- **Order (pedido):** transacción asociada a un cliente, con estado y valor total.
- **Order item (ítem):** producto y cantidad solicitada dentro de un pedido.
- **Customer (cliente):** propietario del pedido, identificado con el formato `C-{número}`.
- **Product (producto):** artículo de catálogo con precio e inventario, identificado con el formato `P-{número}`.

## Estructura de modelos

Los modelos se organizan por responsabilidad y sus namespaces siguen la misma estructura:

```text
Models/
├── Domain/       # Modelos internos del dominio y persistencia en memoria
├── Requests/     # Contratos de entrada, separados por recurso
│   ├── Customers/
│   └── Orders/
├── Responses/    # Contratos JSON devueltos por la API
└── Results/      # Resultados internos de las operaciones de servicio
```

## Endpoints

| Método | Ruta | Parámetros | Respuesta exitosa | Descripción |
|---|---|---|---|---|
| `POST` | `/orders` | Body `CreateOrderRequest` | `201 Created` | Crea un pedido y devuelve su URL en `Location`. |
| `GET` | `/orders/{orderId}` | Path `orderId` | `200 OK` | Obtiene un pedido por su identificador. |
| `GET` | `/orders` | Query opcional `customerId` | `200 OK` | Lista pedidos; permite filtrar por cliente. |
| `PUT` | `/orders/{orderId}` | Path `orderId`, body `UpdateOrderRequest` | `200 OK` | Reemplaza el pedido y recalcula su total e inventario. |
| `DELETE` | `/orders/{orderId}` | Path `orderId` | `200 OK` | Marca el pedido como `DELETED`, devuelve su representación y repone el inventario. |
| `POST` | `/customers` | Body `CreateCustomerRequest` | `201 Created` | Crea un cliente. |
| `GET` | `/customers/{customerId}` | Path `customerId` | `200 OK` | Obtiene un cliente por su identificador. |
| `GET` | `/customers` | — | `200 OK` | Lista todos los clientes. |
| `PUT` | `/customers/{customerId}` | Path `customerId`, body `UpdateCustomerRequest` | `200 OK` | Reemplaza los datos del cliente. |
| `DELETE` | `/customers/{customerId}` | Path `customerId` | `204 No Content` | Elimina un cliente sin pedidos asociados. |

Ejemplo requerido de path parameter: `GET /orders/O-9831`.

Ejemplo requerido de query parameter: `GET /orders?customerId=C-1024`.

## Contratos JSON

### Pedidos

`POST /orders` y `PUT /orders/{orderId}` utilizan la misma estructura de entrada:

```json
{
  "customerId": "C-1024",
  "items": [
    {
      "productId": "P-500",
      "quantity": 2
    }
  ]
}
```

Las operaciones que crean, consultan o actualizan un pedido retornan:

```json
{
  "id": "O-9831",
  "customerId": "C-1024",
  "status": "CREATED",
  "total": 129.90
}
```

`GET /orders` y `GET /orders?customerId=C-1024` retornan una colección:

```json
[
  {
    "id": "O-9831",
    "customerId": "C-1024",
    "status": "CREATED",
    "total": 129.90
  }
]
```

`DELETE /orders/{orderId}` realiza una eliminación lógica y retorna el pedido actualizado:

```json
{
  "id": "O-9831",
  "customerId": "C-1024",
  "status": "DELETED",
  "total": 129.90
}
```

El pedido continúa disponible para consulta. Repetir la operación retorna el mismo resultado sin reponer nuevamente el inventario.

### Clientes

`POST /customers` y `PUT /customers/{customerId}` utilizan:

```json
{
  "fullName": "Ana Pérez",
  "email": "ana.perez@example.com"
}
```

Las operaciones que crean, consultan o actualizan un cliente retornan:

```json
{
  "id": "C-1025",
  "fullName": "Ana Pérez",
  "email": "ana.perez@example.com"
}
```

`GET /customers` retorna una colección:

```json
[
  {
    "id": "C-1024",
    "fullName": "Cliente Demo",
    "email": "cliente1024@example.com"
  },
  {
    "id": "C-1025",
    "fullName": "Ana Pérez",
    "email": "ana.perez@example.com"
  }
]
```

`DELETE /customers/{customerId}` responde `204 No Content` y no contiene cuerpo JSON.

### Errores

Todos los errores comparten `code`, `message` y `correlationId`:

```json
{
  "code": "PRODUCT_NOT_AVAILABLE",
  "message": "Product P-500 does not have sufficient stock.",
  "correlationId": "ab23f8b1-91c2-45dc"
}
```

Los errores de validación agregan opcionalmente `details` con los mensajes asociados a cada campo:

```json
{
  "code": "INVALID_REQUEST",
  "message": "The request contains invalid data.",
  "correlationId": "ab23f8b1-91c2-45dc",
  "details": {
    "Items": [
      "At least one item is required."
    ]
  }
}
```

Los principales códigos de negocio son:

| Código | Estado HTTP | Descripción |
|---|---|---|
| `ORDER_NOT_FOUND` | `404` | El pedido solicitado no existe. |
| `ORDER_ALREADY_DELETED` | `409` | El pedido está eliminado y no puede modificarse. |
| `CUSTOMER_NOT_FOUND` | `404` | El cliente solicitado o asociado no existe. |
| `PRODUCT_NOT_AVAILABLE` | `409` | El producto no existe o no tiene inventario suficiente. |
| `CUSTOMER_EMAIL_ALREADY_EXISTS` | `409` | El correo ya pertenece a otro cliente. |
| `CUSTOMER_HAS_ORDERS` | `409` | El cliente no puede eliminarse porque tiene pedidos. |
| `INVALID_CUSTOMER_ID` | `400` | El identificador usado como filtro no tiene el formato esperado. |
| `INVALID_REQUEST` | `400` | La solicitud no supera las validaciones de entrada. |
| `INTERNAL_ERROR` | `500` | Ocurrió un error inesperado en el servidor. |

## Decisiones de diseño

1. **Recursos plurales y sustantivos en las rutas.** Se usa `/orders` y no verbos como `/createOrder`; el método HTTP expresa la acción y mantiene una nomenclatura REST predecible.
2. **`201 Created` y encabezado `Location`.** Al crear un pedido, la respuesta indica explícitamente que nació un recurso y entrega la dirección canónica para consultarlo.
3. **Errores uniformes y trazables.** Cada fallo devuelve un código estable para el consumidor, un mensaje legible y un `correlationId`. El cliente puede enviar `X-Correlation-Id`; de no hacerlo, el servidor genera uno y siempre lo retorna también como encabezado.
4. **Separación entre path y query parameters.** `orderId` identifica un recurso concreto, mientras `customerId` modifica el filtro de una colección sin crear rutas ad hoc.
5. **Importes decimales.** Los precios y totales se calculan con `decimal` para evitar errores de precisión binaria propios de valores de punto flotante.
6. **Almacenamiento en memoria para el taller.** El alcance se mantiene autocontenido y ejecutable sin infraestructura. Los datos y el inventario se reinician cada vez que arranca la aplicación; en producción se sustituiría por persistencia transaccional.
7. **Integridad entre clientes y pedidos.** Un pedido solo puede usar un cliente existente y un cliente con pedidos no puede eliminarse. Esto evita referencias huérfanas.
8. **Eliminación lógica e inventario reversible.** Al eliminar un pedido se cambia su estado a `DELETED` en lugar de borrar su historial. El inventario se repone una sola vez y la respuesta devuelve el recurso actualizado.

## Ejecución local

Requisitos: .NET SDK 10.

```bash
dotnet restore
dotnet run
```

La URL HTTP configurada es `http://localhost:5237`. El contrato entregable y revisable es `openapi.yaml`.

Productos disponibles al iniciar: `P-500` (25 unidades), `P-501` (50) y `P-502` (10).

## Pruebas con Postman

1. Importe `postman/API.postman_collection.json`.
2. La API inicia con el cliente `C-1024`; también puede ejecutar **Crear cliente**, cuyo script guarda el identificador en `customerId`.
3. Ejecute **Crear pedido** para guardar el identificador retornado en `orderId`.
4. Ejecute las consultas, actualizaciones y eliminaciones. La variable `baseUrl` tiene como valor inicial `http://localhost:5237`.

## Reflexión Final
¿Qué ventaja obtiene el sistema consumidor al disponer de un contrato OpenAPI antes de que el proveedor haya terminado de implementar la API?

El sistema consumidor puede comenzar su desarrollo antes de que la API esté terminada, porque el contrato OpenAPI define de forma precisa los endpoints, parámetros, estructuras de datos, códigos de estado y posibles errores. También puede generar clientes, crear mocks y preparar pruebas de integración anticipadamente. Esto permite que consumidor y proveedor trabajen en paralelo, reduce ambigüedades y disminuye el riesgo de errores durante la integración.
