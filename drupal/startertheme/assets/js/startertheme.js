/**
 * @file
 * startertheme.js
 */

(function ($) {

  Drupal.behaviors.startertheme = {
    attach: function (context, settings) {

      // Attach pretty forms stuff.
      Drupal.behaviors.startertheme.prettyForms(context, settings);

    },
    prettyForms: function(context, settings) {
      $(document).foundation();

      // Borrowed from: http://webdesignerwall.com/tutorials/cross-browser-html5-placeholder-text
      if(!Modernizr.input.placeholder) {

        $('[placeholder]', context)
          .focus(function() {
            var input = $(this);
            if (input.val() == input.attr('placeholder')) {
              input.val('');
              input.removeClass('placeholder');
            }
          })
          .blur(function() {
            var input = $(this);
            if (input.val() == '' || input.val() == input.attr('placeholder')) {
              input.addClass('placeholder');
              input.val(input.attr('placeholder'));
            }
          })
          .blur();

        $('[placeholder]', context)
          .parents('form')
          .submit(function() {
            $(this).find('[placeholder]').each(function() {
              var input = $(this);
              if (input.val() == input.attr('placeholder')) {
                input.val('');
              }
            });
          });

      }
    },
  }

})(jQuery);
