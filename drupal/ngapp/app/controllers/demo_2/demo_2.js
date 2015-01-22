var c2 = ['$rootScope', 'LocalStorage', function($rootScope, LocalStorage) {
  // Bind this to vm (view-model).
  var vm = this;

  vm.doThing = function() {
    $rootScope
      .$emit('c1_message_change', {
        message: 'C2 sent C1 this random number via an Angular $rootScope event! ' + Math.floor((Math.random()*6)+1)
      });
  };

  $rootScope
    .$on('c2_message_change', function($event, data) {
      $event.stopPropagation();
      vm.message = data.message;
    });
}];

$ngapp
  .controller('c2', c2);
