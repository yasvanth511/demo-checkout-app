#!/usr/bin/env bash
set -e

RESOURCE_GROUP=${1:-"rg-checkout-demo"}
LOCATION=${2:-"eastus"}
APP_NAME=${3:-"app-checkout-api"}
ENV_NAME="env-checkout-demo"

echo "=========================================="
echo " Deploying Demo App to Microsoft Azure   "
echo "=========================================="

az account show > /dev/null 2>&1 || az login

echo "1. Creating Resource Group: $RESOURCE_GROUP..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" -o table

echo "2. Creating Container App Environment: $ENV_NAME..."
az containerapp env create --name "$ENV_NAME" --resource-group "$RESOURCE_GROUP" --location "$LOCATION" -o table

echo "3. Building and deploying $APP_NAME..."
az containerapp up \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --environment "$ENV_NAME" \
  --source . \
  --ingress external \
  --target-port 8080 \
  --env-vars "Payment__TimeoutSeconds=3"

FQDN=$(az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" --query "properties.configuration.ingress.fqdn" -o tsv)

echo "=========================================="
echo " Deployment Complete!"
echo " App URL: https://$FQDN"
echo " Swagger: https://$FQDN/swagger"
echo " Metrics: https://$FQDN/metrics"
echo " Health:  https://$FQDN/health"
echo "=========================================="
