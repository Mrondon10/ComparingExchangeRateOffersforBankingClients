# Exchange Rate System - Comparing Exchange Rate Offers for Banking Clients

Sistema que consulta múltiples APIs de tipos de cambio en paralelo y selecciona automáticamente la mejor oferta para clientes bancarios que realizan remesas internacionales.

- Repositorio: [ComparingExchangeRateOffersforBankingClients](https://github.com/Mrondon10/ComparingExchangeRateOffersforBankingClients)
- Tecnologías: .NET 9, C# 13, ASP.NET Core Web API, xUnit, Moq, FluentAssertions, Swagger, Docker



## Objetivo del proyecto

- Consultar en paralelo múltiples proveedores de tipo de cambio (formatos JSON simple, XML, JSON complejo).
- Tolerar fallos de proveedores individuales (timeout, excepciones) y seguir operando.
- Elegir automáticamente la mejor oferta basada en el mayor monto convertido.
- Entregar una base sólida con pruebas unitarias, mocks, Docker y buenas prácticas.

---

## Arquitectura (Clean Architecture)

Patrón: Clean Architecture 


## Características clave

- Ejecución paralela
  - Task.WhenAll para consultar todos los proveedores en simultáneo..

- Tolerancia a fallos 
  - Timeout por proveedor.
  - Manejo de excepciones a nivel de proveedor.
  - El sistema responde mientras al menos 1 proveedor sea válido.

- Selección automática de la mejor tasa
  - Ordena por monto convertido y toma el mayor.

- Soporte multi-formato de APIs
  - JSON simple, XML, JSON anidado.

- Proveedores
  - 3 proveedores mock.
  - Facilita desarrollo, pruebas rápidas sin depender de APIs reales.

- Swagger
  - Documentación.

- Docker y Docker Compose

---

- Principios SOLID aplicados

---

## Tecnologías y herramientas

- Framework: .NET 9.0
- Lenguaje: C# 13
- Web: ASP.NET Core Web API
- Testing:
  - xUnit
  - Moq
  - FluentAssertions
- Documentación: Swagger
- Contenedores:
  - Docker
  - Docker Compose
- Logging: Microsoft.Extensions.Logging

---

## Endpoints y ejemplos

1) POST /api/exchangerate/json  
Content-Type: application/json

Request:
```json
{
  "from": "USD",
  "to": "EUR",
  "value": 100
}
```

Response:
```json
{
  "rate": 00.0
}
```

2) POST /api/exchangerate/xml  
Content-Type: application/xml

Request:
```xml
<XmlRequest>
  <From>USD</From>
  <To>EUR</To>
  <Amount>100</Amount>
</XmlRequest>
```

Response:
```xml
<XML>
  <Result>00.0</Result>
</XML>
```

3) POST /api/exchangerate/exchange  
Content-Type: application/json

Request:
```json
{
  "exchange": {
    "sourceCurrency": "USD",
    "targetCurrency": "EUR",
    "quantity": 100
  }
}
```

Response:
```json
{
  "statusCode": "200",
  "message": "Éxito",
  "data": {
    "total": 00.0
  }
}
```

## Rendimiento y resiliencia

- Paralelismo (Task.WhenAll).
- Timeout por proveedor: 10s.
- Manejo de excepciones individual por proveedor.
- Sistema responde si al menos 1 proveedor retorna resultado válido.

---

## Logging

- Logging con niveles:
  - Information, Debug, Warning, Error
- Trazabilidad por petición: inicio/fin, proveedores consultados, tiempos, errores manejados.

---

## Buenas prácticas aplicadas

- Dependency Injection: todo inyectado por constructor.
- Async/Await de punta a punta.
- CancellationToken soportado.
- Null-safety: validaciones exhaustivas.
- Manejo de errores: try-catch por proveedor y a nivel de servicio.
- Clean Code: nombres descriptivos, métodos pequeños, responsabilidades claras.
- .gitignore: excluye bin/, obj/ y temporales.

---
