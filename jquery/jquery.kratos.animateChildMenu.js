/**
 * @file
 * jquery.kratos.animateChildMenu.js
 */

// Menu animation plugin. To trigger usage from secondary menu links:
// $('ul.menu.sub > li > a').animateChildMenu({});
(function($) {

  $.animateChildMenu = {
    defaults : {
      speed : 400,
      useSample : false
    },
    sample : '<ul class="menu"><li class="first"><a href="3-col.php">Link</a></li><li><a href="3-col.php">Link</a></li><li><a href="3-col.php">Link</a></li><li><a href="3-col.php">Link</a></li><li class="last"><a href="3-col.php">Link</a></li></ul>'
  };

  $.fn.extend({
    animateChildMenu: function(settings) {

      settings.wrapper = $(this).closest(this.selector.split(' ')[0]);
      settings = $.extend({}, $.animateChildMenu.defaults, settings);

      function click(evt) {
        evt.preventDefault();

        var msettings = $.data(this, 'animateChildMenu');

        var $self = $(this);
        var $item = $self.closest('li');
        var $list = $item.closest('ul');
        var $menu = $item.find('ul').eq(0);

        if (msettings.useSample && !$menu.length) {
          $menu = $item
            .append($.animateChildMenu.sample)
            .find('ul');

          // Recursively bind new sample menu.
          init($menu.find('a'), msettings);
        }

        if ($menu.length) {
          if (!$menu.is(':visible')) {
            $self
              .removeClass('close')
              .addClass('open');

            $list
              .find('ul').slideUp(msettings.speed, function() {
                var $tree = $item.parentsUntil(msettings.wrapper);
                var $scrub = (!$tree.length ? msettings.wrapper : $list);

                $scrub
                  .find('li')
                  .removeClass('active-trail')
                  .find('span.toggle')
                  .removeClass('open')
                  .addClass('close');

                $item
                  .addClass('active-trail')
                  .find('span.toggle')
                  .removeClass('close')
                  .addClass('open');
              });

            $menu
              .slideDown(msettings.speed);
          }
          else {
            $self
              .removeClass('open')
              .addClass('close');

            $menu
              .show()
              .slideUp(msettings.speed, function() {
                msettings.wrapper
                  .find('li').removeClass('expand');

                $(this)
                  .find('li')
                  .removeClass('active-trail')
                  .find('ul')
                  .hide()
                  .find('span.toggle')
                  .removeClass('open');

                $item
                  .removeClass('active-trail');
              });
          }
        }
      }

      function init(element, settings) {
        return element.each(function() {

          var $self = $(this);
          var $toggler = $self.find('span.toggle');
          if (!$toggler.length) {
            $toggler = document.createElement('span');
            $.data($toggler, 'animateChildMenu', settings);

            $self
              .append($toggler)
              .find('span')
              .addClass('toggle close')
              .click(click);
          }
        });
      }

      return init(this, settings);
    }
  });

})(jQuery);
