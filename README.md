# Checkout API Demo App (Azure + Grafana Ready)

A clean, production-grade microservice designed for the **ContextEngine CEO Demo**:
- **Endpoints**: `POST /api/checkout`, `GET /api/orders/{id}`, `GET /api/orders`, `/health`, `/metrics`
- **Database**: PostgreSQL (Azure Database for PostgreSQL Flexible Server or In-Memory)
- **Telemetry**: OpenTelemetry with Prometheus `/metrics` scraping endpoint
- **Azure Ready**: Azure Container Apps / App Service deployment scripts
- **Grafana Ready**: Pre-built dashboard and alert rule (`grafana-dashboard.json`)

---

## 1. Run Locally

```bash
dotnet restore
dotnet run
```
* **Swagger UI**: http://localhost:5000/swagger
* **Prometheus Metrics**: http://localhost:5000/metrics
* **Health Check**: http://localhost:5000/health

---

## 2. Deploy to Microsoft Azure (One Command)

### PowerShell (Windows):
```powershell
.\deploy-azure.ps1 -ResourceGroup "rg-checkout-demo" -Location "eastus" -AppName "app-checkout-api"
```

### Bash (macOS/Linux):
```bash
chmod +x deploy-azure.sh
./deploy-azure.sh rg-checkout-demo eastus app-checkout-api
```

This will output your public Azure HTTPS URL (e.g. `https://app-checkout-api.greenbush-012345.eastus.azurecontainerapps.io`).

---

## 3. Connect to Grafana

1. In your Grafana instance (local or Grafana Cloud), add Prometheus data source pointing to your app's `/metrics` endpoint.
2. In Grafana, click **Dashboards** -> **New** -> **Import**, and upload `grafana-dashboard.json`.
3. You now have live real-time metrics for:
   - Checkout RPS throughput
   - P95 / P99 Latency
   - Payment Gateway Timeout Errors & Alerts!
