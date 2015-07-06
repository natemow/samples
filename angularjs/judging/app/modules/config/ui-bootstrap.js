/**
 * @file
 * JS code for Competition Judging.
 */

(function() {
  'use strict';

  // Service to provide shortcut functions for ui.boostrap instance configs.
  var jguiBootstrapUi = function() {
    this.$get = ['$q', '$rootScope', '$modal',
      function($q, $rootScope, $modal) {
        var service = {};

        // Function to return a modal instance promise.
        var openModal = function(scope, templateUrl) {
          // Clear system alerts.
          $rootScope.$emit('modalOpened');

          var modalInstance = $modal.open({
            animation: true,
            size: 'lg',
            templateUrl: templateUrl,
            scope: scope,
            controller: ['$scope', '$modalInstance',
              function($scope, $modalInstance) {
                $scope.modalAnimating = false;

                // Close/submit the modal.
                $scope.modalSubmit = function() {
                  $scope.modalAnimating = true;

                  $modalInstance.close();
                  $rootScope.$emit('modalClosed');
                };
                // Dismiss/cancel the modal.
                $scope.modalDismiss = function() {
                  $scope.modalAnimating = true;

                  $modalInstance.dismiss();
                  $rootScope.$emit('modalClosed');
                };
              }]
          });

          return modalInstance.result;
        };

        // Functions to handle global alerts.
        var arrAlerts = [];
        var alerts = {
          add: function(msg, type) {
            arrAlerts.push({
              type: (type || 'success'),
              msg: msg
            });

            $rootScope.$emit('alertsPrint', arrAlerts);
          },
          clear: function() {
            arrAlerts = [];
            $rootScope.$emit('alertsClear');
          }
        };

        // Listeners for core, contrib or custom events that should always
        // clear alerts.
        var alertsClearEvents = [
          '$routeChangeStart',
          'filtersChanged',
          'pagerClicked',
          'sortChanged',
          'modalOpened'
        ];

        angular.forEach(alertsClearEvents, function(ekey) {
          var listenerClear = $rootScope.$on(ekey, function(e) {
            $rootScope.$emit('alertsClear');
          });
          $rootScope.$on('$destroy', listenerClear);
        });

        // Return service definition.
        var service = {
          openModal: openModal,
          alerts: alerts
        };

        return service;
      }
    ];
  };

  // Filter to use on paged ng-repeat element.
  // e.g. ng-repeat="row in data | orderBy: actions.sort.predicate:actions.sort.reverse | paginationChange:pager.currentPage | limitTo:pager.itemsPerPage"
  var FilterPaginationChange = ['paginationConfig',
    function(paginationConfig) {
      return function(data, currentPage) {
        if (!angular.isArray(data)) {
          return false;
        }

        var start = (!currentPage ? 0 : ((currentPage - 1) * paginationConfig.itemsPerPage))

        return data.slice(start, (start + paginationConfig.itemsPerPage));
      };
    }];

  // Filter used by DirectiveAlerts to group messages in to type groups.
  var FilterAlertType = [
    function() {
      return function(data, type) {
        var alerts = [];
        angular.forEach(data, function(alert) {
          if (alert.type === type) {
            alerts.push(alert);
          }
        });

        return alerts;
      };
    }];

  var DirectiveAlerts = [
    function() {
      return {
        template: '\
          <div id="alerts">\n\
            <div ng-repeat="(type, alerts) in alertGroups">\n\
              <div ng-if="alerts.data.length" class="clearfix alert alert-{{ type }}">\n\
                <button type="button" class="close" ng-click="alerts.close()">\n\
                  <span aria-hidden="true">×</span><span class="sr-only">Close</span>\n\
                </button>\n\
                <p ng-if="(alerts.data.length == 1)" ng-bind-html="alerts.data[0].msg"></p>\n\
                <ul ng-if="(alerts.data.length > 1)"><li ng-repeat="alert in alerts.data" ng-bind-html="alert.msg"></li></ul>\n\
              </div>\n\
            </div>\n\
          </div>',
        controller: ['$rootScope', '$scope', '$filter', 'jguiConfigData', 'jguiUtility',
          function($rootScope, $scope, $filter, jguiConfigDataProvider, jguiUtilityProvider) {

            $scope.alertGroups = {};

            var listenerClear = $rootScope.$on('alertsClear', function(e, animate) {
              e.stopPropagation();

              if (!animate) {
                $scope.alertGroups = {};
              }
              else {
                // Animate out alerts.
                if (!$scope.alertsAnimating) {
                  $scope.alertsAnimating = true;
                  jguiUtilityProvider
                    .jqElement('#alerts')
                    .stop()
                    .show()
                    .delay(jguiConfigDataProvider.settings.alerts.autoFadeTimeout)
                    .fadeOut(jguiConfigDataProvider.settings.animation.duration, function() {
                      $scope.alertGroups = {};
                      $scope.alertsAnimating = false;
                    });
                }
              }
            });
            $scope.$on('$destroy', listenerClear);

            // Group the alerts by type and output 1 container per.
            var listenerPrint = $rootScope.$on('alertsPrint', function(e, data) {
              angular.forEach(['danger', 'warning', 'success'], function(type) {
                var alerts = $filter('alertType')(data, type);
                if (alerts.length) {
                  $scope.alertGroups[type] = {
                    data: alerts,
                    close: function() {
                      $scope.alertGroups[type] = undefined;
                    }
                  };
                }
              });

              $scope.$emit('alertsClear', true);
            });
            $scope.$on('$destroy', listenerPrint);

          }]
      };
    }];

  // Bind the module.
  angular
    .module('jguiBootstrapUI', [
      'ui.bootstrap',
      'jguiConfig'
    ])
    // Override ui.bootstrap.rating constant.
    .constant('ratingConfig', {
      max: 5,
      stateOn: 'fa fa-gavel fa-fw',
      stateOff: 'fa fa-gavel fa-gavel-off fa-fw'
    })
    // Override ui.bootstrap.pagination constant.
    .constant('paginationConfig', {
      itemsPerPage: 25,
      maxSize: 10,
      rotate: false,
      boundaryLinks: true,
      firstText: 'First',
      lastText: 'Last',
      directionLinks: false,
      previousText: 'Prev',
      nextText: 'Next'
    })
    .filter('paginationChange', FilterPaginationChange)
    .filter('alertType', FilterAlertType)
    .provider('jguiBootstrapUi', jguiBootstrapUi)
    .directive('alerts', DirectiveAlerts);

}());
