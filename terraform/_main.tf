provider "azurerm" { 
    version = "~> 2.2.0"
}

terraform {
    backend "azurerm" {}
}