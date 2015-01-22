var LocalStorage = ['$window', function($window) {
  var factory = {};

  factory.get = function(key) {
    return $window.localStorage.getItem(key);
  };

  factory.set = function(key, data) {
    if (data) {
      return $window.localStorage.setItem(key, data);
    }
    else {
      return $window.localStorage.removeItem(key);
    }
  };

  return factory;
}];

var $ngapp = angular
  .module(Drupal.settings.ngapp.name, ['ngAnimate'])
  .factory('LocalStorage', LocalStorage);
