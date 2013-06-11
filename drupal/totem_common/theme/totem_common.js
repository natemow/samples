/**
 * @file
 * totem_common.js
 */
(function($) {

  //////////////////////////////////////////////////////////////////////////////
  // Apply "active" class+indicator onload and on AJAX callback.
  //////////////////////////////////////////////////////////////////////////////
  $.fn.ajaxContentActiveIndicator = function(reset) {
    sender = $(this);
    wrapper = sender.parents('.pager-entity');
    if (reset) {
      wrapper.parents('.pager-group').find('.pager-data .pager-entity').removeClass('active');
      wrapper.addClass('active');
    }

    // Add html indicator in case themers want to use for special positioned
    // icon or something.
    var indicator = $('div.active-indicator');
    if (!indicator.length) {
      indicator = wrapper.parents('.pager-group').append('<div class="active-indicator"></div>');
      indicator = $('div.active-indicator');
    }
  }

  // Magical fadey content effect exec'd when data is AJAX'd in
  // @see totem_common_node_community()
  $.fn.ajaxContentEffect = function(path, data) {

    var content = $('#block-system-main > .block-inner > .content');
    content.fadeOut(400, function() {
      content.html(data);
      content.fadeIn(400);

      // Toggle "active" class on parent items.
      var sender = $('.pager-group a[rel*="' + path + '"]');
      sender.ajaxContentActiveIndicator(true);

      // Rebind behaviors to newly AJAX'd data elements.
      Drupal.attachBehaviors(content);
    });
  };

  // Find current .active element and force indicator prepend.
  // @see totem_common_preprocess_node()
  var activeCurrent = $('.pager-group a[rel*="' + window.location.pathname + '"]');
  if (activeCurrent.length) {
    activeCurrent.ajaxContentActiveIndicator(false);
  }
  //////////////////////////////////////////////////////////////////////////////

  totem_common = {
    // Utility functions.
    util : {
      // Function to scroll $elem to top of $container.
      // $container is the scrollable element, regardless of whether
      // jScrollPane is used on it or not.
      scrollToTop : function($container, $elem) {
        if ($elem.length) {
          var containerIsPage = !$container;

          if (!$container) {
            if ($.browser.safari) {
              $container = $('body');
            }
            else {
              $container = $('html');
            }
          }

          // Are we dealing with jScrollPane?
          if ($container.parents('.custom-scroll').length || $container.hasClass('custom-scroll')) {
            // Did it get applied yet?
            var jspApi = $container.data('jsp');
            if (jspApi) {
              if (jspApi.getIsScrollableV()) {
                // scrollToElement(ele, stickToTop, animate)
                // @see http://jscrollpane.kelvinluck.com/api.html#scrollToElement
                jspApi.scrollToElement($elem, true, true);
              }
            }
            else {
              // Bind to our custom event, because 'jsp-initialised' happens
              // before the JSP API gets attached.
              // @see Drupal.behaviors.totem_common.applyPrettyScroll()
              $container.bind('totem-jsp-applied', {'elem' : $elem}, function(event) {
                var jspApi = $(this).data('jsp');
                if (jspApi.getIsScrollableV()) {
                  jspApi.scrollToElement(event.data.elem, true, true);
                }
              });
            }
          }
          else {
            var scrollTop;
            if (containerIsPage) {
              // Just scroll page to where the element is on it.
              scrollTop = $elem.offset().top;
            }
            else {
              // Compute position of element relative to top of container.
              scrollTop = $elem.offset().top - $container.offset().top;
            }

            $container.animate({ 'scrollTop' : scrollTop }, 1000);
          }

        }
      },

      // Scroll window just enough to make sure $elemFirst,
      // and optionally $elemLast, are wholly visible.
      scrollIntoView : function($elemFirst, $elemLast) {
        var $w = $(window);
        var visibleTop = $w.scrollTop();
        var visibleBottom = visibleTop + $w.height();

        var elemTop = $elemFirst.offset().top;

        if (!$elemLast) {
          $elemLast = $elemFirst;
        }
        // outerHeight() includes padding, border, and margin if arg is true.
        // offset().top finds position of element below any top margin it has,
        // so just subtract the overlap of top margin.
        var elemBottom = $elemLast.offset().top + $elemLast.outerHeight(true) - parseInt($elemLast.css('margin-top'));

        var diff = 0;

        // Note we check for top out of view first. If in some strange case
        // the element(s) are taller than the visible window area, at least
        // the top will be in view.
        if (elemTop < visibleTop) {
          // Top out of view. Diff is negative; scroll up.
          diff = elemTop - visibleTop;
        }
        else if (elemBottom > visibleBottom) {
          // Bottom out of view. Diff is positive; scroll down.
          diff = elemBottom - visibleBottom;
        }

        if (diff) {
          var $page = $.browser.safari ? $('body') : $('html');
          $page.animate({ 'scrollTop' : visibleTop + diff }, 500);
        }

      }
    }
  };

  // Attach behaviors to Drupal.
  Drupal.behaviors.totem_common = {
    attach : function(context, settings) {

      // Set up tooltips.
      Drupal.behaviors.totem_common.applyTooltips(context, settings);

      // Add jScrollPane.
      Drupal.behaviors.totem_common.applyPrettyScroll(context, settings);

      // Tweak CTools modal output.
      Drupal.behaviors.totem_common.modalAdjustments(context, settings);

      // Auto-load of modal windows.
      Drupal.behaviors.totem_common.modalAutoload(context, settings);

      // Apply "View more" paging.
      Drupal.behaviors.totem_common.pagingViewMore(context, settings);

      // Auto-scroll to .active element in fixed height .pager-data elements.
      Drupal.behaviors.totem_common.pagingAutoScroll(context, settings);

      // Utility function to check/uncheck .form-type-checkboxes inputs.
      Drupal.behaviors.totem_common.toggleChecked(context, settings);

      // Dumb #messages tricks.
      Drupal.behaviors.totem_common.adjustMessages(context, settings);

      // Focus/blur form input values, defaulting to label.
      var focusBlurContainers = ['#zone-header', '#zone-footer'];
      $.each(focusBlurContainers, function(ix, selector) {
        Drupal.behaviors.totem_common.applyTxtFocusBlur.init($(selector));
      });

      // Bind the select to filter recent activity on submit.
      Drupal.behaviors.totem_common.bindRecentActivityFilter(context, settings);

      // Expand and collapse rows on Communities list.
      Drupal.behaviors.totem_common.bindCommunitiesExpand(context, settings);

      // TODO.
      Drupal.behaviors.totem_common.nodeLinksAjaxHistory(context, settings);

      // TODO.
      Drupal.behaviors.totem_common.modalFormBtnSubmitting(context, settings);
    },
    adjustMessages : function(context, settings) {

      var messages = $('#messages');
      if (messages.length) {

        // Set auto fadeout according to Drupal variable.
        // @see totem_totem_common_preprocess_page()
        // @see settings.php
        if (Drupal.settings.variables.totem_common.autofade_messages) {
          messages.delay(5000).fadeOut('slow');
        }

        // We only add close link if it's not already there.
        if (messages.children('a#messages-close').length < 1) {

          var close = document.createElement('a');
              close.id = 'messages-close';
              close.href = '#';
              close.innerHTML = 'close';
              close.title = 'close';
              close = $(close);

          messages.prepend(close);

          close.click(function(evt) {
            evt.preventDefault();
            messages.fadeOut(500);
          });
        }
      }

    },
    applyTooltips : function(context, settings) {

      // Bind tooltip plugin to links w/title attribute.
      $('ul.contextual-links a[title]', context).tooltip({
        track: true,
        delay: 0,
        showURL: false,
        showBody: true,
        bodyHandler: function() {
          return '<h3>' + this.innerHTML + '</h3><p>' + this.tooltipText + '</p>';
        },
        fixPNG: true,
        opacity: 0.95,
        fade: 250,
        left: -120
      });

    },
    applyPrettyScroll : function(context, settings) {
      var $pagerData = $('.custom-scroll .pager-data');

      // JSP does not include the height of the absolutely-positioned
      // contextual links in its calculation, so if they're taller than the
      // node teaser, it won't scroll down far enough for those on the last one.
      $pagerData.each(function() {
        // Note: 'this' is the current DOM element in iteration of $pagerData.
        // @see http://api.jquery.com/each/
        var $last = $(this).find('.pager-entity').eq(-1);
        var $ct = $last.find('.contextual-links-wrapper');
        if ($ct.length) {
          var diff = $ct.height() - $last.height();
          if (diff > 0) {
            $last.css('margin-bottom', diff);
          }
        }
      });

      // jScrollPane() constructor method returns the jQuery object upon which
      // it was executed.
      // NOTE: an apparent bug in height calculations was applying jScrollPane
      // when the content height wasn't actually overflowing. This is patched
      // via a CSS rule (that ends up getting overridden)...
      // @see totem_common.css
      var $jspElem = $pagerData.jScrollPane({showArrows:true,verticalDragMaxHeight: 100});
      // Now trigger a custom event indicating not only that JSP is applied,
      // but that the API object is also attached to the element.
      // This is similar to JSP's 'jsp-initialised' event, but that event
      // is triggered BEFORE the API object is attached via data(), meaning
      // that handlers bound to 'jsp-initialised' cannot access the API on the
      // element. (So annoying.)
      // @see http://jscrollpane.kelvinluck.com/script/jquery.jscrollpane.js -
      // end initialise() and return of JScrollPane().
      $jspElem.trigger('totem-jsp-applied');
    },
    modalAdjustments : function(context, settings) {

      // Additional actions on click of a ctools-use-modal link.
      $('a.ctools-use-modal', context).click(function(evt) {
        var self = $(this);
        var modalContent = $('#modalContent');
        if (modalContent.length) {
          // Add a custom class to #modalContent to allow per-form resizing via
          // CSS. See totem_ui/template.php:totem_ui_link for "rel" set.
          modalContent.addClass(self.attr('rel'));
          // Removed all that fixed position stuff...scroll to top of modal
          // instead.
          totem_common.util.scrollIntoView(modalContent, null);
        }
      });

      // Skip the full page refresh for Cancel actions.
      $('.modal-content .form-actions a.cancel', context).not('.totem-media-ctools-reuse-modal').click(function(evt) {
        evt.preventDefault();
        // Instead of calling Drupal.CTools.Modal.dismiss() directly, use
        // modal's close link to also run any handlers bound to its click event.
        $(this).parents('.ctools-modal-content').find('.modal-header a.close').click();
      });

    },
    modalAutoload : function(context, settings) {

      // Detect a request for autoload of modal (i.e. ?modal=/path/to/modal).
      if (settings.variables.totem_common.autoload_modal) {
        var link = $('a[href^="' + settings.variables.totem_common.autoload_modal + '"]', context);
        link.eq(0).once('autoload-modal', function() {
          $(this).click();
        });
      }

    },
    pagingViewMore : function(context, settings) {

      $('.pager-group ul.pager', context).hide();

      // Set up "View more" buttons for first click action.
      $('a.pager-view-more', context).each(function(ix, e) {
        var self = $(e);
        var next = self.parents('.pager').find('ul.pager > li.pager-next > a');

        self.attr('href', next.attr('href'));
      });

      // Bind "View more" button click event.
      $('a.pager-view-more', context).click(function(evt) {
        evt.preventDefault();

        var self = $(this);
        var target = self.attr('href');

        // Prevent user from double-clicking while waiting on response below.
        if (self.hasClass('loading')) {
          return false;
        }

        self.addClass('loading');
        self.attr('rel', escape(self.html()));
        self.html('Loading More...<span></span>');

        var xhr = $.get(target, {}, function(response) { });

        xhr.success(function(response) {
          var html = $(response);
          // Node the ">" immediate child selectors; this is key to account for
          // nested .pager-entity elements.
          var items = html.find('#' + self.attr('id')).parents('.pager-group').find('.pager-data > .pager-entity');
          var last = self.parents('.pager-group').find('.pager-data > .pager-entity:last');

          // Add new items after current last item.
          last.after(items);

          // Attach any behaviors to new content, now that it's in context.
          Drupal.attachBehaviors(items);

          // Scroll to first item returned.
          var first = items.first();
          var pager_data = self.parents('.pager-group').find('.pager-data');
          if (pager_data.css('overflow-y') == 'scroll' || pager_data.css('overflow-y') == 'auto') {
            totem_common.util.scrollToTop(pager_data, first);
          }
          else {
            totem_common.util.scrollToTop(null, first);
          }

          // Update "View more" button target href.
          var next = html.find('#' + self.attr('id')).parents('.pager').find('ul.pager > li.pager-next > a');
          if (next.length) {
            self.attr('href', next.attr('href'));
          }
          else {
            self.attr('href', '#');
            self.hide();
          }

          // Re-enable click.
          self.removeClass('loading');
          self.html(unescape(self.attr('rel')));
          self.removeAttr('rel');
        });

        xhr.error(function(response) {

        });

        xhr.complete(function(response) {

        });

      });

    },
    pagingAutoScroll : function(context, settings) {
      var pager_group = $('.pager-group', context);
      if (pager_group.length) {
        var pager_data = pager_group.find('.pager-data:first-child');
        var active = pager_data.find('.pager-entity.active');
        totem_common.util.scrollToTop(pager_data, active);
      }
    },
    toggleChecked : function(context, settings) {

      var link = $('a.toggle-checked', context);
      if (link.length) {
        link.click(function(evt) {
          evt.preventDefault();

          var checkboxes = link.parents('.form-item.form-type-checkboxes');
          if (checkboxes.length) {
            checkboxes = checkboxes.find('.form-checkboxes input[type="checkbox"]');
            if ($(checkboxes[0]).attr('checked')) {
              checkboxes.attr('checked', '');
              link.text('Select all');
            }
            else {
              checkboxes.attr('checked', 'checked');
              link.text('Select none');
            }
          }
        });
      }

    },
    applyTxtFocusBlur : {
      // Class to handle text input focus/blur effect
      focus : function(e) {
        var self = $(this);
        var value = self.val();

        if (value) {
          self.attr('rel', self.val())
        }

        self.val('');
        self.addClass('focused');
      },
      blur : function(e) {
        var self = $(this);
        var value = self.val();

        if (!value) {
          self.val(self.attr('rel'));
        }

        self.removeClass('focused');
      },
      init : function(container) {
        if (!container.length) {
          return false;
        }

        var e = container.find('input[type="text"], textarea');

        e.each(function(ix) {
          var self = $(this);
          var label = self.parents('.form-item').find('label');
          var value = self.val();

          if (!value) {
            self.val(label.text());
            self.attr('rel', self.val());
          }
        });

        e.focus(this.focus);
        e.blur(this.blur);
        e.parents('form').find('input[type="submit"]').click(function() {
          e.each(function(ix) {
            var self = $(this);
            if (self.val() == self.attr('rel')) {self.val('');}
          });
        });
      }
    },
    bindRecentActivityFilter: function(context, settings) {
      $('select#edit-ra-filter').change(function() {
        $(this).parents('form').submit();
      });
    },
    bindCommunitiesExpand: function(context, settings) {
      // Use .once() b/c modal fails to attach behaviors to its context only.
      // @see /modules/contrib/ctools/js/modal.js, line 286
      // @see http://drupal.org/node/1851108
      // Also, we can't use table#communities in selector along with context,
      // because when new rows are added by AJAX, that markup (i.e. context)
      // won't contain the outer table.
      $('td.name .community-expand', context).once('modalattachfail', function() {
        $(this).click(function() {
          var $btn = $(this);
          // Note that jQuery slide effects do not work properly on
          // table-related elements.
          // @see totem-common-page-communities.tpl.php
          var $rowInner = $(this).parents('.pager-entity').find('.details-wrapper');
          if ($rowInner.is(':visible')) {
            $rowInner.slideUp(400, function () {
              $btn.text('+');
            });
          }
          else {
            $rowInner.slideDown(400, function () {
            $btn.html('&ndash;');
            });
          }
        }).css('cursor', 'pointer');
      });
    },
    nodeLinksAjaxHistory: function(context, settings) {
      var History = window.History;
      if (!History.enabled) {
        return;
      }

      var $ajaxNodeLinks = $('.pager-data .node-teaser .node-title a.use-ajax', context);
      if (!$ajaxNodeLinks.length) {
        return;
      }

      var $w = $(window);

      // On first load of a Community tab, the path may not include active nid.
      // In this case, content defaults to first query result.
      // @see totem_common_node_community()
      // For state-change handling, we store this nid in window's data.
      var parts = window.location.pathname.split('/');
      if (!parseInt(parts[parts.length-1])) {
        var matches = $('.pager-data .node-teaser.active').eq(0).attr('id').match(/-[0-9]+/);
        var nid = parseInt(matches[0].substr(1));
        $w.data('defaultNid', nid);
      }

      $ajaxNodeLinks.once('ajax-history', function () {
        $(this).click(function() {
          var $a = $(this);
          if ($a.hasClass('ajax-processed')) {
            // If window statechange handler did not set this to 'triggered',
            // it must be a real click by user.
            if (!$a.data('method')) {
              $a.data('method', 'clicked');
            }

            // Only push state if link was actually clicked.
            // If click handler was triggered by window statechange handler,
            // then state was pushed by browser button; don't update.
            if ($a.data('method') == 'clicked') {
              var rel = $a.attr('rel');
              // If node title is in document title, replace it.
              var nodeFullTitle = $('#region-content .node-full .node-title').text();
              var docTitle = document.title.replace(nodeFullTitle, $a.attr('title'));
              History.pushState({ 'rel' : rel }, docTitle, rel);
            }
          }
        });
      });

      // Cannot use once() on the window object, because it's based on
      // adding a class, which doesn't work for window.
      if (!$w.data('statechangeBound')) {
        // Both History.pushState() and browser back/forward buttons
        // trigger the statechange event on the window:
        // History.pushState() - user has just clicked AJAXified link.
        // Browser back/forward - we need to update the page.
        $w.bind('statechange', function() {
          var rel = "";
          var state = History.getState();
          if (state.data.rel) {
            rel = state.data.rel;
          }
          else {
            // If state data does not exist, it means History.pushState() wasn't
            // called to create this state, i.e. we have reached the original
            // page-load state.
            rel = window.location.pathname;
            // Attach default nid if exists.
            if ($w.data('defaultNid') != undefined) {
              rel = rel + '/' + $w.data('defaultNid');
            }
          }

          // Find desired AJAXified link.
          var $a = $('a[rel="' + rel + '"].ajax-processed');
          if ($a.length) {
            if ($a.data('method') == 'clicked') {
              // If link was clicked, the AJAX request is already being handled.
              // Reset method for future clicks.
              $a.data('method', false);
            }
            else {
              // Browser button; need to incur the AJAX request.
              $a.data('method', 'triggered');
              $a.click();
            }
          }

        });
        $w.data('statechangeBound', true);
      }
    },
    modalFormBtnSubmitting: function(context, settings) {
      $('form.ctools-use-modal-processed input.form-submit', context).click(function() {
        var $btn = $(this);

        // Replace the value with an "active" word.
        // Interestingly, if form submit returns with error, the value is
        // magically restored to the original. I will thank CTools and not
        // wonder why. ;)
        var text = $btn.attr('value');
        var replacements = {
          'Save' : 'Saving...',
          'Delete' : 'Deleting...',
          'Remove' : 'Removing...',
          'Send' : 'Sending...',
          'Report' : 'Reporting...',
          'Leave' : 'Leaving...',
          'Sign' : 'Signing in...',
          'Register' : 'Submitting...'
        };
        for (var prop in replacements) {
          if (text.indexOf(prop) !== -1) {
            $btn.attr('value', replacements[prop]);
            break;
          }
        }

        // Since AJAX completes before the background page reload does,
        // add a custom class to ensure we keep the button looking
        // disabled/in-progress until reload.
        $btn.addClass('modal-form-submitting');
      });
    }
  }

})(jQuery);
