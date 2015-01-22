var c1 = ['$rootScope', 'LocalStorage', function($rootScope, LocalStorage) {
  // Bind this to vm (view-model).
  var vm = this;

  vm.doThing = function() {
    $rootScope
      .$emit('c2_message_change', {
        message: 'C1 sent C2 this random number via an Angular $rootScope event! ' + Math.floor((Math.random()*6)+1)
      });
  };

  $rootScope
    .$on('c1_message_change', function($event, data) {
      $event.stopPropagation();
      vm.message = data.message;
    });
}];

$ngapp
  .controller('c1', c1);
