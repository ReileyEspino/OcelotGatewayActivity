**Course:** System Integration and Architecture II
**Activity:** API Gateway with Ocelot (routing, JWT protection, rate limiting)

## Overview

This project puts an Ocelot API Gateway in front of three small ASP.NET services. Clients never talk to a service directly. They send every request to the gateway on port 5000, and the gateway decides whether to forward it, block it, or throttle it.

The gateway does three jobs:

1. **Routing:** it maps public `/gateway/...` paths to the matching internal service.
2. **Authentication:** it checks JWT bearer tokens before forwarding any protected request.
3. **Throttling:** it caps how often the login endpoint can be called.

## How the Pieces Fit

```
                       +--------------------------+
   Client  ----------> |  Ocelot Gateway  :5000   |
 (browser / Postman)   |  - route matching        |
                       |  - JWT check             |
                       |  - rate limit            |
                       +------------+-------------+
                                    |  (internal only)
              +---------------------+---------------------+
              |                     |                     |
     Products Service        Orders Service         Auth Service
         :5001                   :5002                  :5003
```

| Component        | Address                 | Public? |
|------------------|-------------------------|---------|
| API Gateway      | http://localhost:5000   | Yes     |
| Products Service | http://localhost:5001   | No      |
| Orders Service   | http://localhost:5002   | No      |
| Auth Service     | http://localhost:5003   | No      |

## Route Policy at a Glance

| Gateway path               | Methods           | Goes to  | Guard                          |
|----------------------------|-------------------|----------|--------------------------------|
| `/gateway/products`        | GET               | :5001    | Open                           |
| `/gateway/products/{id}`   | GET               | :5001    | Open                           |
| `/gateway/products`        | POST, PUT, DELETE | :5001    | JWT required (`GatewayKey`)    |
| `/gateway/orders`          | GET               | :5002    | JWT required (`GatewayKey`)    |
| `/gateway/auth/login`      | POST              | :5003    | Rate limit: 5 calls per 10 s   |

The rule of thumb: reading the catalog is public, changing it needs a token, and orders are always private.

## Running the Lab

Open four terminals (or four Visual Studio startup projects) and start each service on its own port:

```bash
# Terminal 1: Products
cd ProductsService
dotnet run --urls "http://localhost:5001"

# Terminal 2: Orders
cd OrdersService
dotnet run --urls "http://localhost:5002"

# Terminal 3: Auth
cd AuthService
dotnet run --urls "http://localhost:5003"

# Terminal 4: Gateway (start last)
cd ApiGateway
dotnet run --urls "http://localhost:5000"
```

Then send all test traffic to `http://localhost:5000`.

## Test Results (Part C)

Each case was sent through the gateway. The observed status matched the expected status every time.

| # | Request                            | Token | Expected | Observed          | What it shows |
|---|------------------------------------|-------|----------|-------------------|---------------|
| 1 | `GET /gateway/products`            | None  | 200      | 200 OK            | The product list is public. |
| 2 | `GET /gateway/products/1`          | None  | 200      | 200 OK            | A single product is public. |
| 3 | `POST /gateway/auth/login`         | None  | 200      | 200 OK            | Login works and returns a JWT. |
| 4 | `POST /gateway/products`           | None  | 401      | 401 Unauthorized  | Stopped at the gateway: no bearer token. |
| 5 | `POST /gateway/products`           | Valid | 201      | 201 Created       | Token accepted, request reached Products Service. |
| 6 | `GET /gateway/orders`              | None  | 401      | 401 Unauthorized  | Stopped at the gateway: no bearer token. |
| 7 | `GET /gateway/orders`              | Valid | 200      | 200 OK            | Token accepted, request reached Orders Service. |
| 8 | `POST /gateway/auth/login` (x6, rapid) | None | 429  | 429 Too Many Requests | The 6th call inside 10 s was refused by the rate limiter. |

## Evaluation Question

**Which requests does the gateway reject before they reach a service, and why?**

The gateway turns away two kinds of requests on its own, so the downstream services never see them.

**1. Missing or invalid token on a protected route (401 Unauthorized)**

- *Routes:* `POST`, `PUT` and `DELETE` on `/gateway/products`, and `GET /gateway/orders`.
- *Why:* These routes carry an `AuthenticationOptions` block with the provider key `GatewayKey` in `ocelot.json`. Ocelot validates the `Authorization: Bearer <JWT>` header first. If the header is absent, malformed or expired, the gateway answers 401 immediately. Neither the Products Service nor the Orders Service receives the call.

**2. Too many login attempts (429 Too Many Requests)**

- *Route:* `POST /gateway/auth/login`.
- *Why:* The route has a rate-limit rule of 5 calls per 10-second window. Once a client goes past that, Ocelot's rate-limiting middleware replies 429 without contacting the Auth Service. This protects the login endpoint from brute-force guessing.

Public `GET` routes for products have neither guard, so they pass straight through.

## Gateway Configuration (`ocelot.json`)

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/gateway/products",
      "UpstreamHttpMethod": [ "GET" ],
      "DownstreamScheme": "http",
      "DownstreamPathTemplate": "/api/products",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5001 }
      ]
    },
    {
      "UpstreamPathTemplate": "/gateway/products/{id}",
      "UpstreamHttpMethod": [ "GET" ],
      "DownstreamScheme": "http",
      "DownstreamPathTemplate": "/api/products/{id}",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5001 }
      ]
    },
    {
      "UpstreamPathTemplate": "/gateway/products",
      "UpstreamHttpMethod": [ "POST", "PUT", "DELETE" ],
      "DownstreamScheme": "http",
      "DownstreamPathTemplate": "/api/products",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5001 }
      ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "GatewayKey"
      }
    },
    {
      "UpstreamPathTemplate": "/gateway/orders",
      "UpstreamHttpMethod": [ "GET" ],
      "DownstreamScheme": "http",
      "DownstreamPathTemplate": "/api/orders",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5002 }
      ],
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "GatewayKey"
      }
    },
    {
      "UpstreamPathTemplate": "/gateway/auth/login",
      "UpstreamHttpMethod": [ "POST" ],
      "DownstreamScheme": "http",
      "DownstreamPathTemplate": "/api/auth/login",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5003 }
      ],
      "RateLimitOptions": {
        "EnableRateLimiting": true,
        "Period": "10s",
        "PeriodTimespan": 10,
        "Limit": 5,
        "ClientWhitelist": []
      }
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

## Takeaways

- A gateway gives clients one address, so internal ports and service layout stay hidden.
- Checking tokens at the gateway means each service does not have to repeat the same security code.
- Rate limiting on the login route blocks abusive traffic before it uses any Auth Service resources.
