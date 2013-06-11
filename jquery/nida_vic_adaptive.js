(function ($) {
    function vsort_clickTrigger() {
        $('.view .form-submit').trigger("click");
    }

    //dynamic photo caption sizing -- window.load because certain browsers load images after document ready
    $(window).load(function () {
        
		/* equalize height on stupid boxes on homepage */
		
		$(".front block-views-trends-and-statistics-block-1 .teaser").each(function () {
            var specialheight = $("#block-views-nida-notes-blocks-block-7 .view-nida-notes-blocks > .view-content").height();
			var specialheightminus = specialheight - 96;
            $(".front block-views-trends-and-statistics-block-1 .teaser").css("height", specialheightminus);
        });		
		
		/*functions for dynamic image container resizing */
		
		$('.node-type-infographics #zone-content div.field-field-stats-image').each(function () {
            var width = $(this).find('img').width();
            $(this).css("width", width);
        });

        $('#zone-content div.center.border').each(function () {
            var width = $(this).find('img').width();
            $(this).css("width", width);
        });

        $('#zone-content div.left.border').each(function () {
            var width = $(this).find('img').width();
            $(this).css("width", width);
        });

        $('#zone-content div.right.border').each(function () {
            var width = $(this).find('img').width();
            $(this).css("width", width);
        });

    });

    Drupal.behaviors.nida_vic_adaptive = {
        attach:function (context, settings) {

            // Resize main nav item widths.
            Drupal.behaviors.nida_vic_adaptive.resizeMainNav(context, settings);

            // Alter the DOM a bit for "state00" responsive layout.
            Drupal.behaviors.nida_vic_adaptive.alterResponsiveState00(context, settings);

            // Alter the DOM a bit for "state01" responsive layout.
            Drupal.behaviors.nida_vic_adaptive.alterResponsiveState01(context, settings);

            // Landing page rotators.
            Drupal.behaviors.nida_vic_adaptive.cycleLandingPageSlides(context, settings);

            $('#otherSitesCarousel').jcarousel({
                wrap:'circular',
                scroll:4
            });

            $('#otherSitesCarousel .jcarousel-prev').jcarouselControl({
                target: '-=4'
            });

            $('#otherSitesCarousel .jcarousel-next').jcarouselControl({
                target: '+=4'
            });

            $('.mobile-main-nav h2').click(function () {
                /* $(this).parent().parent().find('.view-content').css('height','126px'); */
                $(this).siblings('div.content').find('ul.menu:first-child').toggle();
                $(this).toggleClass('opened')
            });

            $('.mobile-section-nav h2').click(function () {
                /* $(this).parent().parent().find('.view-content').css('height','126px'); */
                $(this).siblings('div.content').find('ul.menu:first-child').toggle();
                $(this).toggleClass('opened')
            });

            //content slideshow

            if($('ul.slideshow').length) {
                $('ul.slideshow').before('<div id="slideshownav" class="nav"></div>').cycle({
                    fx:'fade',
                    speed:1000,
                    timeout:0,
                    pager:'#slideshownav',
                    pagerAnchorBuilder:function (idx, slide) {
                        var src = $('img', slide).attr('src');
                        return '<div><a href="#"><img src="' + src + '"width="120" height="80" /></a></div>';

                    }
                });
            }

            //video slideshow
            $('.videoslideshow > div').css({
                top:-9001
            });

            if($('div.videoslideshow').length) {
                $('div.videoslideshow').cycle({
                    fx:'fade',
                    speed:1000,
                    timeout:0,
                    pager:'#videoslideshownav',
                    pagerAnchorBuilder:function (idx, slide) {
                        return '#videoslideshownav div:eq(' + idx + ') a';
                    }
                });
            }

            if($('div.videoslideshow-block').length) {
                $('div.videoslideshow-block').cycle({
                    fx:'fade',
                    speed:1000,
                    timeout:0,
                    pager:'#videoslideshownav-block',
                    pagerAnchorBuilder:function (idx, slide) {
                        return '#videoslideshownav-block div:eq(' + idx + ') a';
                    }
                });
            }

            //Zebra striping for tables
            $("#region-content .content tr:nth-child(odd)").addClass("odd");
            $("#region-content .content tr:nth-child(even)").addClass("even");

            //add the magnfying lens on enlargeable images
            $("a.colorbox").append("<span></span>");

            $("a.colorbox").hover(function () {
                $(this).children("span").fadeIn(600);
            }, function () {
                $(this).children("span").fadeOut(200);
            });

            //publication view sort active links
            if($("#edit-sort-by").val() == 'field_date_value')
                $('.publishDateSort').addClass('active');
            else
                $('.titleSort').addClass('active');

            //publication view sort listeners
            $('.publishDateSort').click(function () {
                $("#edit-sort-by option[value='field_date_value']").attr("selected", "selected");

                if($("#edit-sort-by").val() == 'field_date_value' && $("#edit-sort-order").val() == 'ASC')
                    $("#edit-sort-order option[value='DESC']").attr("selected", "selected");
                else
                    $("#edit-sort-order option[value='ASC']").attr("selected", "selected");

                vsort_clickTrigger();
            });

            $('.dateSort').click(function () {
                $("#edit-sort-by option[value='field_revisiondate_value']").attr("selected", "selected");

                if($("#edit-sort-by").val() == 'field_revisiondate_value' && $("#edit-sort-order").val() == 'ASC')
                    $("#edit-sort-order option[value='DESC']").attr("selected", "selected");
                else
                    $("#edit-sort-order option[value='ASC']").attr("selected", "selected");

                vsort_clickTrigger();
            });

            $('.titleSort').click(function () {
                $("#edit-sort-by option[value='title']").attr("selected", "selected");

                if($("#edit-sort-by").val() == 'title' && $("#edit-sort-order").val() == 'ASC')
                    $("#edit-sort-order option[value='DESC']").attr("selected", "selected");
                else
                    $("#edit-sort-order option[value='ASC']").attr("selected", "selected");

                vsort_clickTrigger();
            });

            $('#block-block-160').append('<div id="is"></div> ');
            $('#block-block-160 div#is').load('/is.php');
        },
        resizeMainNav:function (context, settings) {

            // Main menu equal item width.
            var calculateMainNavWidths = function () {
                var mm_items = $('#region-main-menu ul.sf-menu > li > a', context);
                if (mm_items.length) {

                    var mm_width_total = $('#region-main-menu', context).width();
                    var mm_width_first = $(mm_items[0]).outerWidth();
                    var mm_width_available = (mm_width_total - mm_width_first);

                    // Get px width - first item.
                    var mm_item_width_pixels = Math.floor(mm_width_available / (mm_items.length - 1));
                    var mm_item_width_percent = ( (mm_item_width_pixels * 100) / mm_width_available );

                    //mm_items.parents('li:not(:first-child)').css({ 'width' : mm_item_width_percent + '%' });
                    mm_items.parents('li:not(:first-child)').css({'width':mm_item_width_pixels + 'px'});
                    mm_items.parents('li:first-child').css({'width':mm_width_first + 'px'});
                }
            };

            // Calculate main nav item widths on load.
            calculateMainNavWidths();
            // Calculate main nav item widths on resize.
            $(window).resize(calculateMainNavWidths);

        },
        alterResponsiveState:function (context, settings, args) {

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
        alterResponsiveState00:function (context, settings) {

            /**
             * TODO: Don't love duplicating these min/max values from the Omega media
             * query here -- only alternatives I can see right now are:
             *
             *   A) Do some ugly string parsing of
             *   settings.omega.layouts.queries.state01
             *
             *   B) Check theme vars on the back end and use drupal_add_js to supply
             *   min/max settings values...not sure that data would be available
             *   there either, though.
             *
             *   C) Weight JS differently so that Omega first has a chance to apply
             *   the body.responsive-layout-STATEKEY class; in that case, no data
             *   passing is even necessary and any layout changes needed could just
             *   be based on simple selectors.
             *
             */

            // Add settings data for this responsive state.
            var args = {
                'widthMin':1,
                'widthMax':600,
                'callback':function (active) {

                    var page_tools = $('#getArticle', context);

                    if (page_tools.length) {
                        if (active) {
                            // Move blocks to custom main content grid column.
                            if (!$('#region-content > .region-inner > #getArticle', context).length) {
                                page_tools.detach();
                                page_tools.appendTo('#region-content > .region-inner');
                            }
                        }
                        else {
                            // Reset and move blocks back to main right column position 0.
                            if ($('#region-content > .region-inner > #getArticle', context).length) {
                                page_tools.detach();
                                page_tools.prependTo('#region-right-column > .region-inner');
                            }
                        }
                    }

                }
            };

            // Call shared state checker.
            Drupal.behaviors.nida_vic_adaptive.alterResponsiveState(context, settings, args);

        },
        alterResponsiveState01:function (context, settings) {

            // Add settings data for this responsive state.
            var args = {
                'widthMin':601,
                'widthMax':768,
                'callback':function (active) {

                    // Do some custom layout stuff for these homepage blocks.
                    var blocks_left = $('body.front #region-right-column #getArticle, body.front #region-right-column .block.director, body.front #region-right-column .block.promotions', context);
                    var blocks_right = $('body.front #region-right-column .block.block-170, body.front #region-right-column .block.block-226', context);

                    if (blocks_left.length && blocks_right.length) {
                        if (active) {
                            // Move blocks to custom left/right grid columns.
                            if (!$('#region-right-column > .region-inner > .group-leftcolumn', context).length) {

                                $('#region-right-column > .region-inner').prepend('<div class="group-leftcolumn"></div><div class="group-rightcolumn"></div>');

                                blocks_left.detach();
                                blocks_left.appendTo('#region-right-column > .region-inner > .group-leftcolumn');
                                blocks_right.detach();
                                blocks_right.appendTo('#region-right-column > .region-inner > .group-rightcolumn');
                            }
                        }
                        else {
                            // Reset and move blocks back to main grid column.
                            if ($('#region-right-column > .region-inner > .group-leftcolumn', context).length) {

                                blocks_left.detach();
                                blocks_left.appendTo('#region-right-column > .region-inner');
                                blocks_right.detach();
                                blocks_right.appendTo('#region-right-column > .region-inner');

                                $('#region-right-column > .region-inner > .group-leftcolumn, #region-right-column > .region-inner > .group-rightcolumn').remove();
                            }
                        }
                    }
                }
            };

            // Call shared state checker.
            Drupal.behaviors.nida_vic_adaptive.alterResponsiveState(context, settings, args);

        },
        cycleLandingPageSlides:function (context, settings) {

            // Cycle plugin options.
            var opts = {
                fx:'fade',
                speed:1000,
                timeout:5000,
                fit:1,
                width:'100%',
                pager:'#nav',
                pagerAnchorBuilder:function (idx, slide) {
                    return '<li><a href="#">' + $(slide).find("div.views-field-field-rotator-image h3.buttonTitle").text() + '</a></li>';
                }
            };

            // Set slide wrapper.
            var slide_wrapper = $('div.view-landing-page-rotators div.view-content', context);

            if (slide_wrapper.length) {
                slide_wrapper.find('img').removeAttr('width').removeAttr('height');

                // Init bind of plugin.
                slide_wrapper
                    .after('<ul id="nav" class="nav"></ul>')
                    .cycle(opts);

                // On window resize, re-bind plugin.
                $(window, context).resize(function () {
                    opts.width = '100%';
                    opts.height = slide_wrapper.height();
                    slide_wrapper.cycle('destroy');
                    slide_wrapper.cycle(opts);
                });
            }

        }
    };
})(jQuery);