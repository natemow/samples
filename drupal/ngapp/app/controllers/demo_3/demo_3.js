var c3 = ['$rootScope', '$http', 'LocalStorage', function($rootScope, $http, LocalStorage) {
  // Bind this to vm (view-model).
  var vm = this;

  // Get filters from local storage.
  var filters = LocalStorage.get('filters');
  if (filters) {
    filters = JSON.parse(filters);
  }
  else {
    filters = {
      field_tags_tid: []
    };
  }

  // Private function for updating vm active filters bools.
  var setFilters = function() {
    vm.filter_1 = (filters.field_tags_tid.indexOf('Term 1') >= 0);
    vm.filter_2 = (filters.field_tags_tid.indexOf('Term 2') >= 0);
    vm.filter_3 = (filters.field_tags_tid.indexOf('Term 3') >= 0);

    var endpoint = '/api/views/dogfood?filters[tags]=' + filters.field_tags_tid.join(',');

    $http
      .get(endpoint)
      .success(function(result) {
        vm.data = result;
        vm.endpoint = endpoint;
      })
      .error(function(result) { });
  };

  // Filter button click action.
  vm.doFilter = function(value) {

    var ix = filters.field_tags_tid.indexOf(value);
    if (ix >= 0) {
      filters.field_tags_tid.splice(ix, 1);
    }
    else {
      filters.field_tags_tid.push(value);
    }

    // Add filter to local storage so its persisted between page refreshes.
    LocalStorage.set('filters', JSON.stringify(filters));
    setFilters();
  };

  // Apply active filter bools to vm.
  setFilters();

}];

$ngapp
  .controller('c3', c3);
