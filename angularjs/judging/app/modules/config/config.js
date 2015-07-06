/**
 * @file
 * JS code for Competition Judging.
 */

(function() {
  'use strict';

  // Service to provide config data.
  var jguiConfigData = function() {
    this.$get = ['$rootScope',
      function($rootScope) {
        var data = ($rootScope.jguiConfigData || false);
        if (!data) {
          $rootScope.jguiConfigData = JSON.parse(angular.element('#jguiConfigData').text());
        }

        return $rootScope.jguiConfigData;
      }];
  };

  // Service to provide meta data.
  var jguiMetaData = function() {
    this.$get = ['$q', '$http', '$rootScope', 'jguiConfigData',
      function($q, $http, $rootScope, jguiConfigDataProvider) {
        var service = {};

        service.getOptions = function() {
          var meta = ($rootScope.metaData || false);

          var deferred = $q.defer();

          if (meta) {
            deferred.resolve($rootScope.metaData);
          }
          else {
            $http
              .get(jguiConfigDataProvider.resources.metadata.options)
              .then(function(response) {
                $rootScope.metaData = response.data.data;

                deferred.resolve($rootScope.metaData);
              });
          }

          return deferred.promise;
        };

        // Return service definition.
        return service;
      }];
  };

  // Service to provide common utility functions.
  var jguiUtility = function() {
    this.$get = ['$log', '$rootScope',
      function($log, $rootScope) {

        // Function to select for a jqlite/jquery object.
        var jqElement = function(selector) {
          return angular
            .element(document.querySelector(selector));
        };

        // Sort an array of objects by key.
        var sortByKey = function(array, key) {
          return array.sort(function(a, b) {
            var x = a[key];
            var y = b[key];

            return ((x < y) ? -1 : ((x > y) ? 1 : 0));
          });
        };

        // Return service definition.
        var service = {
          jqElement: jqElement,
          sortByKey: sortByKey
        };

        return service;
      }
    ];
  };

  // Interceptor for all HTTP requests.
  // Note binding in jguiConfigProviders.
  var jguiInterceptor = function() {
    this.$get = ['$q', function($q) {
      var interceptor = {};

      interceptor.request = function(config) {
        return config;
      };

      interceptor.response = function(response) {
        return response;
      };

      // Forces Angular to stop processing if it received bad HTTP response.
      interceptor.responseError = function(response) {
        return $q.reject(response);
      };

      return interceptor;
    }];
  };

  // Configure providers.
  var jguiConfigProviders = ['$httpProvider', '$compileProvider', '$provide',
    function($httpProvider, $compileProvider, $provide) {
      // Add our custom HTTP interceptor.
      $httpProvider
        .interceptors
        .push('jguiInterceptor');

      // Apply some performance boosts to versions gte 1.3.
      // @see https://keyholesoftware.com/2014/11/17/new-features-in-angularjs-1-3
      if (angular.version.major == 1 && angular.version.minor >= 3) {
        $httpProvider
          .useApplyAsync(true);

        $compileProvider
          .debugInfoEnabled(false);
      }

    }];


  // Bind the module.
  angular
    .module('jguiConfig', [])
    .provider('jguiInterceptor', jguiInterceptor)
    .provider('jguiConfigData', jguiConfigData)
    .provider('jguiMetaData', jguiMetaData)
    .provider('jguiUtility', jguiUtility)
    .config(jguiConfigProviders);

}());
