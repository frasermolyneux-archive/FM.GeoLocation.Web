resource "azurerm_resource_group" "resource-group" {
    name = "fm-geolocationweb-${var.environment}"
    location = var.region
}
