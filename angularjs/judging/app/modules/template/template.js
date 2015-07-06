/**
 * @file
 * JS code for Competition Judging.
 */

(function() {
  'use strict';

  // Configure routing.
  var jguiConfigRoutes = ['$routeProvider', '$locationProvider', 'jtplTemplate',
    function($routeProvider, $locationProvider, jtplTemplate) {

      // Set route configs.
      var params = {
        // Note that controllerPage may ngInclude an alternate templateUrl
        // if "template" param set in config.
        templateUrl : jtplTemplate + '/default.html',
        controller : 'controllerPage'
      };

      // Use a wildcard router setup.
      $routeProvider
        .when('/', params)
        .when('/:path', params)
        .otherwise('error');

      // Set pretty URLs config using the History API when available; set prefix
      // for fallback hash method.
      $locationProvider
        .html5Mode(true)
        .hashPrefix('!');
    }];

  // Controller to handle main template.
  var ControllerTemplate = ['$scope', 'jguiConfigData',
    function($scope, jguiConfigDataProvider) {
      // Set template var.
      $scope.template = jguiConfigDataProvider.template;
    }];

  // Controller to handle default page/ng-view for all routed requests.
  var ControllerPage = ['$routeParams', '$scope', 'jguiConfigData', 'jtplTemplate',
    function($routeParams, $scope, jguiConfigDataProvider, jtplTemplate) {

      // Check validity of request path per config. Note that the "path"
      // var is defined in ngRoute config.
      // @see jguiConfigRoutes
      var key = $routeParams.path || 'index';
      if (!jguiConfigDataProvider.pages[key]) {
        key = 'error';
      }

      // Set this page's config params.
      var data = jguiConfigDataProvider.pages[key];
      var params = angular.extend($routeParams, data, { key: key });

      if (!params.title) {
        params.title = jguiConfigDataProvider.template.brand;
      }

      // Set template and page vars.
      $scope.template.title = params.title;
      $scope.page = {
        template: (params.template ? jtplTemplate + '/' + params.template : null),
        title: params.title,
        params: params
      };

    }];

  // Directive to handle main menu.
  var DirectiveMenu = ['jguiQueues',
    function(jguiQueuesProvider) {
      return {
        template: '<li ng-repeat="item in queueCounts" ng-class="{ active: item.active }"><a href="{{ item.path }}">{{ item.label }} <span class="badge pull-right">{{ item.count }}</span></a></li>',
        controller: ['$rootScope', '$route', '$location', '$window', 'jguiUtility',
          function($rootScope, $route, $location, $window, jguiUtilityProvider) {

            // Initial binding for counter UI. Service listener sets queueCounts.
            // @see jguiQueues
            $rootScope.$emit('setQueueCounts');

            // Listener for route changes.
            var listenerRouteChange = $rootScope.$on('$routeChangeSuccess', function(e) {
              // Scroll to top of page.
              $window.scrollTo(0, 0);

              // Event to set active menu item.
              $rootScope.$emit('menuActive', $route.current.params.path);
            });
            $rootScope.$on('$destroy', listenerRouteChange);

            // Listener to set active menu item.
            // @see jguiQueues, DirectiveMenu
            var listenterMenuActive = $rootScope.$on('menuActive', function(e, activePath) {
              angular.forEach($rootScope.queueCounts, function(item) {
                item.active = (item.path == activePath);
              });
            });
            $rootScope.$on('$destroy', listenterMenuActive);

            // Bind to window scroll and trigger sticky menu on threshold.
            var header = jguiUtilityProvider.jqElement('header');
            var threshold = header.height();
            angular
              .element($window)
              .bind('scroll', function() {
                if ($window.scrollY >= threshold) {
                  header.addClass('navbar-fixed-top');
                }
                else {
                  header.removeClass('navbar-fixed-top');
                }
              });

          }]
      };
    }];

  // Bind the module.
  angular
    .module('jguiTemplate', [
      'ngRoute',
      'jguiConfig',
      'jguiQueues'
    ])
    .constant('jtplTemplate', 'app/modules/template')
    .config(jguiConfigRoutes)
    .controller('controllerTemplate', ControllerTemplate)
    .controller('controllerPage', ControllerPage)
    .directive('templateMenu', DirectiveMenu);

}());
