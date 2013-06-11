/**
 * @file
 * nprdd-ui.js
 */

// Jquery plugin to apply equal heights to items.
(function($) {
  $.fn.extend({
    equalHeightChildren: function(settings) {

      var $self = $(this);
      var height = 0;

      $self
        .each(function() {
          var currentHeight = $(this).height();
          if (currentHeight > height) {
            height = currentHeight;
          }
        })
        .height(height);
    }
  });
})(jQuery);


(function ($, Drupal, window, document) {

  Drupal.behaviors.nprdd_ui = {
    attach: function(context, settings) {

      // Some settings for theme's behavior functions.
      settings.nprdd_ui = {
        mainMenu : {
          animationSpeed : 300,
          animationComplete : false,
          animationIsAttached : false,
          mobileActiveBreakpoint: 768
        }
      };

      // Bind main menu.
      Drupal.behaviors.nprdd_ui.mainMenu(context, settings);
      // Bind FAQ (and similar) tab behaviors.
      Drupal.behaviors.nprdd_ui.softTabs(context, settings);
      // Adjust jumpnav per active hash.
      Drupal.behaviors.nprdd_ui.jumpNavTweaks(context, settings);
      // Attach show/hide stuff for Calculator widgets.
      Drupal.behaviors.nprdd_ui.calculatorAnimations(context, settings);
      // Pretty Form stuff.
      Drupal.behaviors.nprdd_ui.prettyForms(context, settings);
    },
    alterResponsiveState : function(context, settings, args) {

      var w = $(window, context);

      // Anonymous function to check that current viewport width
      // is in media query range, and if so run a callback function.
      var isStateActive = function () {
        var width_viewport = w.width();
        var active = (width_viewport >= args.widthMin && width_viewport <= args.widthMax);

        if (args.callback) {
          args.callback(active);
        }
      };

      // Determine if state active on load.
      isStateActive();
      // Determine if state active on resize.
      w.resize(isStateActive);

    },
    alterResponsiveStatesAll : function(context, settings) {
      var args = {
        'widthMin' : 1,
        'widthMax' : 980,
        'callback' : function(active) {
          if (active) {

          }
        }
      };

      // Call shared state checker.
      Drupal.behaviors.nprdd_ui.alterResponsiveState(context, settings, args);
    },
    mainMenu: function(context, settings) {

      // Class to recursively apply equal item widths
      // + animate the menu on item hover.
      var mm = {
        cache : $('#main-menu ul.menu', this.context).eq(0),
        cacheMobile : $('#content .main-menu-wrapper', this.context),
        animate : function() {

          // Only bind these events if flag not set.
          if (!mm.settings.animationIsAttached) {
            // Re-queue slide animation if mouseout of wrapper ul.menu.
            this.cache.hover(
              function(evt) { },
              function(evt) {
                mm.settings.animationComplete = false;
              }
            );

            // Handle mouseover/out for individual ul.menu items.
            this.cache.children('li').hover(
              function(evt) {
                var $self = $(this);
                var $menu = $self.children('ul.menu');

                if ($menu.length) {
                  mm.cache
                    .removeClass('no-sub')
                    .addClass('has-sub');

                  if (mm.settings.animationComplete) {
                    $menu
                      .stop(true, true)
                      .css({
                        'display': 'none'
                      })
                      .show();
                  }
                  else {
                    $menu
                      .stop(true, true)
                      .fadeIn({
                        duration: mm.settings.animationSpeed,
                        queue: false
                      })
                      .css({
                        'display': 'none'
                      })
                      .slideDown(mm.settings.animationSpeed, function() {
                        mm.settings.animationComplete = true;
                      });
                  }
                }
                else {
                  mm.cache
                    .removeClass('has-sub')
                    .addClass('no-sub');

                  mm.settings.animationComplete = false;
                }

              },
              function(evt) {
                mm.cache
                  .removeClass('has-sub')
                  .addClass('no-sub');

                $(this)
                  .children('ul.menu')
                  .stop(true, true)
                  .hide();
              }
            );

            // Mobile menu.
            $('#mobile-menu-toggle a.toggler', this.context).click(function(evt) {
              evt.preventDefault();

              var $self = $(this);
              if (!$self.hasClass('expanded')) {
                mm.cacheMobile
                  .stop(true, true)
                  .slideDown(mm.settings.animationSpeed, function() { });

                $self.addClass('expanded');
              }
              else {
                mm.cacheMobile
                  .stop(true, true)
                  .slideUp(mm.settings.animationSpeed, function() { });

                $self.removeClass('expanded');
              }
            });


            mm.settings.animationIsAttached = true;
          }

          return this;
        },
        init : function(context, settings) {

          this.context = context;
          this.settings = settings.nprdd_ui.mainMenu;

          // This fixes an issue switching between mobile and full menus...if
          // mobile has ever been expanded, we need to strip all styles set by
          // its slideUp/slideDown functions.
          $(window).resize(function() {
            mm.cacheMobile
              .removeAttr('style');

			$('#mobile-menu-toggle a.toggler', context)
			  .removeClass('expanded');
          });

          this.animate();
        }
      };

      mm.init(context, settings);
    },
    softTabs: function(context, settings) {

      var options = {
        defaultTab: 'tab1',
        position: 'top-left',
        size: 'large',
        rounded: false,
        theme: 'white',
        shadows: false,
        responsive: true,
        responsiveDelay: 0,
        animation: {
          easing: 'easeInOutExpo',
          duration: 600,
          effects: 'fade',
          type: 'jquery'
        }
      };

      $.each(['vertical', 'horizontal'], function(ix, value) {
        options.orientation = value;
        $('.soft-tabs.' + value, context).zozoTabs(options);
      });

    },
    jumpNavTweaks: function(context, settings) {

      var $links_jumpnav = $('.views-menu-nodes-jump-targets .menu-jump-subnav a', context);

      function getHashParams() {
        var hashParams = {};
        var e,
          a = /\+/g,  // Regex for replacing addition symbol with a space
          r = /([^&;=]+)=?([^&;]*)/g,
          d = function (s) { return decodeURIComponent(s.replace(a, " ")); },
          q = window.location.hash.substring(1);

        while (e = r.exec(q))
           hashParams[d(e[1])] = d(e[2]);

        return hashParams;
      }

      function setActive(current) {
        $links_jumpnav.removeClass('active');
        current.addClass('active');
      }

      // Set active from initial hash request.
      var hashes_current = getHashParams();
      $.each(hashes_current, function(key, value) {
        $links_jumpnav.each(function() {
          var $self = $(this);
          if (key == $self[0].hash.substring(1)) {
            setActive($self);
          }
        });
      });

      // Set active for jumpnav link click.
      $links_jumpnav.click(function(evt) {
        setActive($(this));
      });

    },
    calculatorAnimations: function(context, settings) {

      $('.block-tabs .tab').click(function(){
        $('.block-tabs .tab').each(function(){
          $(this).removeClass('active');
        });
        $('.tab-container .pane').each(function(){
          $(this).removeClass('active');
        });
        $(this).addClass('active');
        $('#'+$(this).attr('rel')).addClass('active');
      });

      $('#alignment-embed select').change(function(){
        $('.alignment').each(function(){
          $(this).removeClass('active');
        });
        $('#'+$(this).val()).addClass('active');
      });

    },
    prettyForms: function(context, settings) {
      $(document).foundation();

      // Borrowed from: http://webdesignerwall.com/tutorials/cross-browser-html5-placeholder-text
      if(!Modernizr.input.placeholder){

        $('[placeholder]').focus(function() {
          var input = $(this);
          if (input.val() == input.attr('placeholder')) {
          input.val('');
          input.removeClass('placeholder');
          }
        }).blur(function() {
          var input = $(this);
          if (input.val() == '' || input.val() == input.attr('placeholder')) {
          input.addClass('placeholder');
          input.val(input.attr('placeholder'));
          }
        }).blur();
        $('[placeholder]').parents('form').submit(function() {
          $(this).find('[placeholder]').each(function() {
          var input = $(this);
          if (input.val() == input.attr('placeholder')) {
            input.val('');
          }
          })
        });

      }
    }
  };

})(jQuery, Drupal, this, this.document);
