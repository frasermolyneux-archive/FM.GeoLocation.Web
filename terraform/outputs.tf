output "instrumentation_key" {
  value     = azurerm_application_insights.app-insights.instrumentation_key
  sensitive = true
}

output "app_id" {
  value = azurerm_application_insights.app-insights.app_id
}
