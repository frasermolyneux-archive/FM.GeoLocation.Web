resource "azurerm_resource_group" "resource-group" {
  name     = "fm-geolocationweb-${var.environment}"
  location = var.region
}

resource "azurerm_application_insights" "app-insights" {
  name                 = "fm-geolocationweb-appinsights-${var.environment}"
  location             = azurerm_resource_group.resource-group.location
  resource_group_name  = azurerm_resource_group.resource-group.name
  application_type     = "web"
  daily_data_cap_in_gb = 10
  retention_in_days    = 30
  disable_ip_masking   = true
}
