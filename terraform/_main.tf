provider "azurerm" { 
    version = "~> 1.43"
}

terraform {
    backend "azurerm" {}
}