# Config module

This module provides low-level preconfig for the main module and services to
retrieve config data (`jguiConfigDataProvider`), wrap utility functions
(`jguiUtilityProvider`) and wrappers for UI Bootstrap bindings
(`jguiBootstrapUiProvider`).

## HOWTO

Get config data:

    jguiConfigDataProvider
      .then(function(config) {
        // Do stuff with JSON config data.
      })
      .then(function() {
        // Do some other stuff.
      });

Clear and add global alerts:

    jguiBootstrapUiProvider.alerts.clear();

    // 2nd type arg can be one of danger|warning|success (success is default).
    jguiBootstrapUiProvider.alerts.add('My warning message', 'warning');

Get a jqLite/jQuery enabled element:

    jguiUtilityProvider
      .jqElement('#alerts .alert.alert-' + type)
      .fadeOut(400, function() {
        $rootScope.alertGroups[type] = [];
      });

Inject the jguiConfig, jguiUtility, jguiBootstrapUi services:

    // Bind the module.
    angular
      .module('jguiWhateverModule', [
        'jguiConfigData',
        'jguiUtility',
        'jguiBootstrapUI',
        ...
      ])
      .provider('jguiWhateverService', jguiWhateverService)
      ...

    // Service to get/set Whatevers.
    var jguiWhateverService = function() {
      this.$get = ['$rootScope', 'jguiConfigData', 'jguiUtility', 'jguiBootstrapUi',
        function($rootScope, jguiConfigDataProvider, jguiUtilityProvider, jguiBootstrapUiProvider) {

          var service = {};

          ...

          return service;
        }];
    };
