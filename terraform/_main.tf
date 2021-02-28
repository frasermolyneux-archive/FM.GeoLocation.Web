provider "azurerm" { 
    version = "~> 2.49.0"
    features {}
}

terraform {
    backend "azurerm" {}
}