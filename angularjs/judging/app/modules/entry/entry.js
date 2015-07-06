/**
 * @file
 * JS code for Competition Judging.
 */

(function() {
  'use strict';

  // Service to get/set data per Entry provided.
  var jguiEntry = function() {
    this.$get = ['$q', '$http', '$rootScope', 'jguiConfigData', 'jguiBootstrapUi', 'jguiQueues',
      function($q, $http, $rootScope, jguiConfigDataProvider, jguiBootstrapUiProvider, jguiQueuesProvider) {

        var service = {
          // Fetch full entry data from server.
          // TODO NEEDS DATA: GET full entry data from server.
          load: function(entry, queue) {
            return $http
              .get(jguiConfigDataProvider.resources.entry.load + '/' + entry.id)
              .then(function(response) {

                // Configure the ui.bootstrap.rating directives for this entry.
                entry.scoring = [];
                angular.copy(jguiConfigDataProvider.settings.scoring.criteria, entry.scoring);
                angular.forEach(entry.scoring, function(criteria) {
                  angular.extend(criteria, {
                    rate: 0,
                    percent: 0,
                    max: jguiConfigDataProvider.settings.scoring.max,
                    // Allow/disallow scoring criteria updates per queue.
                    isReadonly: (queue.key !== 'queue')
                  });
                });

                // Dummy data for notes.
                entry.notes = [];
                for (var n = 10; n > 0; n--) {
                  entry.notes.push({
                    note: "This is note #" + ((10 - n) + 1) + ", fear my judgement.",
                    date: Date.now() - (n * 1000 * 60 * 60)
                  });
                }

                return entry;
              });
          },

          // Adds/removes an entry from all queues. Queue exclusivity (or not) is
          // handled per the queueAssignments args.
          // TODO NEEDS DATA: POST full entry data to server.
          setQueues: function(entry, queueAssignments) {

            jguiBootstrapUiProvider.alerts.clear();

            angular.forEach(queueAssignments, function(status, key) {
              // Get all data from the queue service.
              jguiQueuesProvider
                .getQueueRootScope(key)
                .then(function(queue) {
                  var index = queue.data.indexOf(entry);

                  if (!status && index >= 0) {
                    // Remove current from this queue.
                    queue.data.splice(index, 1);

                    // Broadcast event letting queue directive know it needs to
                    // remove a row.
                    // @see DirectiveQueue
                    $rootScope.$emit('queueRowRemoved', entry);
                    jguiBootstrapUiProvider.alerts.add('Entry #' + entry.id + ' removed from <em>' + queue.label + '</em>.', 'warning');
                  }
                  else if (status && index < 0) {
                    // Add current to beginning of this queue.
                    queue.data.unshift(entry);
                    jguiBootstrapUiProvider.alerts.add('Entry #' + entry.id + ' moved to <em><a href=' + key + '#entry-' + entry.id + '>' + queue.label + '</a></em>.');
                  }
                });
            });

            // Broadcast an event so the counter directive is updated.
            // @see jguiQueues
            $rootScope.$broadcast('setQueueCounts');
          },

          // Sets the score for an entry.
          // TODO NEEDS DATA: POST to server.
          setScore: function(entry) {
            return $http
              .post(jguiConfigDataProvider.resources.entry.load + '/' + entry.id, { entry: entry })
              .then(function(response) {

                jguiBootstrapUiProvider.alerts.clear();

                return entry;
              })
              .then(function(entry) {
                jguiBootstrapUiProvider.alerts.add('Scores for entry #' + entry.id + ' saved.');
                angular.forEach(entry.scoring, function(criteria) {
                  jguiBootstrapUiProvider.alerts.add(criteria.label + ': ' + criteria.rate + ' (' + criteria.percent + '%)');
                });
                jguiBootstrapUiProvider.alerts.add('Notes: ' + entry.newNote);

                return entry;
              });
          }

        };

        return service;
      }];
  };

  var DirectiveEntry = [
    function() {
      return {
        controller: ['$scope', 'jguiBootstrapUi', 'jtplEntry', 'jguiEntry',
          function($scope, jguiBootstrapUiProvider, jtplEntry, jguiEntryProvider) {

            // Define actions to attach to $scope.entry below.
            var actions = {
              setQueues: function(queueTargetKey) {
                var args = {};
                switch (queueTargetKey) {
                  case 'queue':
                    args = {
                      queue: true,
                      attention: false,
                      failure: false
                    };
                    break;

                  case 'attention':
                    args = {
                      queue: false,
                      attention: true,
                      failure: false
                    };
                    break;

                  case 'failure':
                    args = {
                      queue: false,
                      attention: false,
                      failure: true
                    };
                    break;
                }

                jguiEntryProvider.setQueues($scope.entry, args);
              },
              setScore: function() {
                // Entry scored; force presence in main queue.
                $scope.entry.actions.setQueues('queue');

                jguiEntryProvider.setScore($scope.entry);
              },
              setNotesStub: function() {
                var stub = '';
                switch ($scope.queue.key) {
                  case 'queue':
                    angular.forEach($scope.entry.scoring, function(c, ix) {
                      stub += (ix > 0 ? '\n\n' : '') + '*' + c.label + '*\n\n' + 'Add ' + c.label + ' notes here.';
                    });
                    stub += '\n';
                    break;
                }

                $scope.entry.actionNote = stub;
              },
              criteria: {
                toggle: function(c) {
                  $scope.criteriaActive = {};
                  $scope.criteriaActive.label = c.label;
                  $scope.criteriaActive.description = c.description;
                  $scope.criteriaActive.weight = c.weight;
                },
                set: function(c, value) {
                  if (!c.isReadonly) {
                    c.rate = value;
                    c.overStar = value;
                    c.percent = 100 * (value / c.max);

                    $scope.entry.actions.criteria.touch();
                  }
                },
                touch: function() {
                  $scope.criteriaAverage = 0;
                  $scope.criteriaWeights = 0;
                  angular.forEach($scope.entry.scoring, function(c) {
                    $scope.criteriaAverage += (c.percent * (c.weight / 100)) * $scope.entry.scoring.length;
                    $scope.criteriaWeights += c.weight;
                  });

                  $scope.criteriaAverage = $scope.criteriaAverage / $scope.entry.scoring.length;
                },
                animate: {
                  show: function(c) {
                    return (c.rate || c.overStar);
                  },
                  classes: function(c) {
                    return {
                      'label-danger': (c.percent < 30),
                      'label-warning': (c.percent >= 30 && c.percent < 50),
                      'label-info': (c.percent >= 50 && c.percent < 80),
                      'label-success': (c.percent >= 80)
                    };
                  }
                }
              }
            };

            // Listener for queue entry load event.
            // @see DirectiveQueue
            $scope.$on('entryModalLoad', function(e, queue, entry) {

              // Set highlight color on queue row clicked.
              entry.activeModal = true;

              // Fetch full entry data.
              return jguiEntryProvider
                .load(entry, queue)
                .then(function(entry) {
                  $scope.queue = queue;
                  $scope.entry = entry;
                  $scope.entry.actions = actions;

                  // Init criteria average.
                  $scope.entry.actions.criteria.touch();

                  // Configure the ui.bootstrap.tabs directive for this entry.
                  $scope.tabs = [];
                  angular.forEach(['Scoring', 'Notes', 'Documents', 'Miscellaneous'], function(title) {
                    var tab = {
                      title: title
                    };
                    // Set "Notes" as default if this is the main queue.
                    if ($scope.queue.key !== 'queue' && title === 'Notes') {
                      tab.active = true;
                    }

                    $scope.tabs.push(tab);
                  });

                })
                .then(function() {
                  // Open modal, set up submit + dismiss callbacks.
                  return jguiBootstrapUiProvider
                    .openModal($scope, jtplEntry + '/entry.html')
                    .then(
                      function() {
                        $scope.entry.activeModal = false;

                        // Set highlight color on queue row actioned.
                        $scope.entry.statusActionTaken = true;
                      },
                      function() {
                        $scope.entry.activeModal = false;
                      }
                    );
                });
            });

          }]
      }
    }];

  // Bind the module.
  angular
    .module('jguiEntry', [
      'jguiConfig',
      'jguiBootstrapUI',
      'jguiQueues'
    ])
    .constant('jtplEntry', 'app/modules/entry')
    .provider('jguiEntry', jguiEntry)
    .directive('entryModal', DirectiveEntry);

}());
