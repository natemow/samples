/**
 * @file
 * startertheme.js
 */

(function ($) {

  Drupal.behaviors.startertheme = {
    attach: function (context, settings) {

      // Attach someFeature stuff.
      Drupal.behaviors.startertheme.someFeature(context, settings);

    },
    someFeature: function(context, settings) {

      // TODO: do someFeature stuff.

    }
  }

})(jQuery);
