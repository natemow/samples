/**
 * @file
 * JS code for Competition Judging.
 */

(function() {
  'use strict';

  // Service to get/set data per Queue provided.
  var jguiQueues = function() {
    this.$get = ['$q', '$http', '$rootScope', '$route', 'jguiConfigData', 'jguiUtility',
      function($q, $http, $rootScope, $route, jguiConfigDataProvider, jguiUtilityProvider) {

        var service = {};

        $rootScope.allQueues = [];

        // Retrieve queue data per configured endpoint.
        // TODO NEEDS DATA: Fetch full data from server and remove
        // $rootScope.allQueues stuff.
        service.getQueueRootScope = function(name) {
          var deferred = $q.defer();

          if ($rootScope.allQueues && $rootScope.allQueues[name]) {
            deferred.resolve($rootScope.allQueues[name]);

            return deferred.promise;
          }

          $http
            .get(jguiConfigDataProvider.resources.queues[name])
            .then(function(response) {
              var queue = {
                label: jguiConfigDataProvider.pages[name].title,
                link: '<a href="' + name + '">' + jguiConfigDataProvider.pages[name].title + '</a>',
                data: response.data.data
              };

              $rootScope.allQueues[name] = queue;
            })
            .then(function() {
              deferred.resolve($rootScope.allQueues[name]);
            });

          return deferred.promise;
        };

        // Set updated queue counts.
        service.setQueueCounts = function() {

          $rootScope.queueCounts = [];

          angular.forEach(jguiConfigDataProvider.resources.queues, function(path, key) {
            service
              .getQueueRootScope(key)
              .then(function(queue) {
                var item = {
                  path: key,
                  index: jguiConfigDataProvider.pages[key].index,
                  label: queue.label,
                  count: queue.data.length,
                  active: false
                };

                $rootScope.queueCounts.push(item);
              })
              .then(function() {
                // Sort the counts array per page.index key.
                jguiUtilityProvider.sortByKey($rootScope.queueCounts, 'index');
                // Event to set active menu item.
                // @see DirectiveMenu
                $rootScope.$emit('menuActive', $route.current.params.path);
              });
          });
        };

        // Listener to set $rootScope.queueCounts.
        var listenerQueueCounts = $rootScope.$on('setQueueCounts', function(e) {
          service.setQueueCounts();
        });
        $rootScope.$on('$destroy', listenerQueueCounts);

        return service;
      }];
  };

  // Directive to handle queues and item actions.
  var DirectiveQueue = ['jtplQueue', 'jguiConfigData', 'paginationConfig', 'jguiQueues',
    function(jtplQueue, jguiConfigDataProvider, paginationConfig, jguiQueuesProvider) {
      return {
        templateUrl: jtplQueue + '/queue.html',
        link: function(scope, element, attrs) {

          // Do the initial binding for our directive.
          jguiQueuesProvider
            .getQueueRootScope(attrs.name)
            .then(function(obj) {
              scope.queue = {
                key: attrs.name,
                label: obj.label
              };

              return obj.data;
            })
            .then(function(data) {
              // Set scope data and pager object.
              scope.data = data;
              scope.pager = angular.extend(paginationConfig, {
                show: (scope.data.length > paginationConfig.itemsPerPage),
                totalItems: scope.data.length,
                // IMPORTANT: Reset currentPage on each new directive linking.
                /*
                 * Without this, route changes from a path with data page
                 * count gt target route data page count will yield no data
                 * rendering on the target route.
                 */
                currentPage: null
              });

              // Disable paging per config.
              if (!jguiConfigDataProvider.settings.paging.enabled) {
                scope.pager.itemsPerPage = scope.data.length;
                scope.pager.show = false;
              }

            });
        },
        controller: ['$scope', '$window', 'jguiMetaData',
          function($scope, $window, jguiMetaDataProvider) {

            // We need this empty object to bind ng-model values in template.
            // Used to filter corresponding rows in the ng-repeat expression.
            $scope.search = {};

            // Functions to support $scope.search filtering.
            $scope.filters = {
              // Placeholder for populated options data.
              options: {},
              // Toggle the search panel.
              panelToggle: function(open) {
                $scope.filters.panelOpen = open;

                // Set user preference on panel opened/closed.
                $window.localStorage.setItem('queueSearchPanel', JSON.stringify({
                  open: $scope.filters.panelOpen
                }));
              },
              // Reset scope search filter if no value selected.
              change: function(query, key) {
                if (!query[key]) {
                  query[key] = undefined;
                }
              },
              // Do an exact match filter on piped rows; by default, Angular
              // will use a "contains" expression.
              match: function(query, key) {
                return function(row) {
                  if (!query[key]) {
                    return true;
                  }

                  return (row[key] == query[key]);
                }
              }
            };

            // Fetch the search filter options data.
            jguiMetaDataProvider
              .getOptions()
              .then(function(options) {
                $scope.filters.options = options;

                // Persist user preference on panel opened/closed.
                var preferences = $window.localStorage.getItem('queueSearchPanel');
                if (preferences) {
                  preferences = JSON.parse(preferences);

                  $scope.filters.panelToggle(preferences.open);
                }
                else {
                  $scope.filters.panelToggle(true);
                }
              });

            // Controller for queue actions.
            $scope.actions = {
              // Open modal for full actions on the row.
              // @see DirectiveEntry
              loadEntry: function(queue, row) {
                $scope.$broadcast('entryModalLoad', queue, row);
              },
              // Set up queue sorting object.
              sort: {
                predicate: '',
                reverse: true,
                icon: {
                  classes: '',
                  show: function(predicate) {
                    return ($scope.actions.sort.predicate === predicate);
                  }
                },
                order: function(predicate) {
                  $scope.actions.sort.reverse = ($scope.actions.sort.predicate === predicate) ? !$scope.actions.sort.reverse : false;
                  $scope.actions.sort.predicate = predicate;
                  $scope.actions.sort.icon.classes = {
                    'fa fa-caret-down': $scope.actions.sort.reverse,
                    'fa fa-caret-up': !$scope.actions.sort.reverse
                  };

                  // Clear alerts.
                  // @see jguiBootstrapUi
                  $scope.$emit('sortChanged');
                }
              }
            };

          }]
      };
    }];

  // Bind the module.
  angular
    .module('jguiQueues', [
      'jguiConfig',
      'jguiBootstrapUI'
    ])
    .constant('jtplQueue', 'app/modules/queue')
    .provider('jguiQueues', jguiQueues)
    .directive('queue', DirectiveQueue);

}());
