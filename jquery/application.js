

$(document).ready(function()
{
	
	var is_ie6 		= false;
	var is_ie7 		= false;
	
	if (jQuery.browser.msie) {
		if (parseInt(jQuery.browser.version) <= 7) {
			is_ie7 = true;
		}
	}
	
	if (jQuery.browser.msie) {
		if (parseInt(jQuery.browser.version) <= 6) {
			is_ie6 = true;
		}
	}
	
	/* IE7 hackery */
	if (is_ie7) {
		$(function() {
			var zIndexNumber = 1000;
			$('div').each(function() {
				zIndexNumber -= 10;
			});
		});
	}
	
	
/*
 * UTILITY FUNCTIONS
 * 
 * If you wrote a really cool UNIVERSALLY APPLICABLE function, put it here
 * 
 * */
	
	// Pad a number with leading zeros
	var padZero = function(n, len) {
		var s = n.toString();
	    if (s.length < len) {
	        s = ('0000000000' + s).slice(-len);
	    }
	    
	    return s;
	};
	
	// Calculate distance between 2 lat/lng pairs using Haversine
	// (depends on distance_calculator.js)
	var calculateDistance = function(lat1, lon1, lat2, lon2) {
		var R = 6371; // km
		var dLat = (lat2-lat1).toRad();
		var dLon = (lon2-lon1).toRad();
		var lat1 = lat1.toRad();
		var lat2 = lat2.toRad();

		var a = Math.sin(dLat/2) * Math.sin(dLat/2) +
		        Math.sin(dLon/2) * Math.sin(dLon/2) * Math.cos(lat1) * Math.cos(lat2); 
		var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a)); 
		var d = R * c;
		
		return d;
	};
	
	// Class to handle text input focus/blur effect 
		var applyTxtFocusBlur =
		{
			focus : function(e) {
				var self = $(this);
				self.val('');
				self.addClass('focus');
				self.removeClass('blur');
			},
			blur : function(e) {
				var self = $(this);
				if (self.val()=='') {
					self.val( self.attr('rel') );
					self.removeClass('focus');
					self.addClass('blur');
				}
			},
			init : function(e) {
				e.attr('rel', e.val());
				e.focus(this.focus);
				e.blur(this.blur);
			}
		};
		
	// Carousel wrapper class
		var applyCarousel = 
		{
			carousel_initCallback : function(carousel)
			{
				var clickDefault = function(evt) {
					evt.preventDefault();
				};
				
				// User clicked next button
				carousel.options.custom_next.bind('click', function(evt) {
					clickDefault(evt);
					carousel.next();
				});
				// User clicked prev button
				carousel.options.custom_prev.bind('click', function(evt) {
					clickDefault(evt);
					carousel.prev();
				});
			},
			carousel_beforeAnimation : function(carousel, item, ix, state)
			{
				return false;
			},
			carousel_afterAnimation : function(carousel, item, ix, state)
			{
				return false;
			},
			init : function(target, prev, next)
			{
				// Set carousel options
				var opts = {
						buttonNextHTML	: null,
						buttonPrevHTML	: null,
						scroll			: 5,
						auto			: 0,
						wrap			: 'circular',
						initCallback	: this.carousel_initCallback,
						itemFirstInCallback: {
							onBeforeAnimation	: this.carousel_beforeAnimation,
							onAfterAnimation	: this.carousel_afterAnimation
						},
						custom_prev		: prev,
						custom_next		: next
					};
					
				// Bind the carousel
				target.jcarousel(opts);
			}
		};		
		
		
	// Class to paint a Google map, accepts multiple locations
		var paintMap =
		{
			mapTypeId : 'custom',
			styleOptions : { name: 'ScoutScoop' },
			styles : 
			[
				{	featureType	: 'administrative',
					elementType	: 'all',
					stylers		: [{ saturation: -100 }]
				},
				{	featureType	: 'landscape',
					elementType	: 'all',
					stylers		: [{ saturation: -100 }]
				},
				{	featureType	: 'poi',
					elementType	: 'all',
					stylers		: [{ saturation: -100 }]
				},
				{	featureType	: 'road',
					elementType	: 'all',
					stylers		: [{ saturation: -100 }]
				},
				{	featureType	: 'transit',
					elementType	: 'all',
					stylers		: [{ saturation: -100 }]
				},
				{	featureType	: 'water',
					elementType	: 'all',
					stylers		: [{ saturation: -50 }]
				}
			],
			setIcon : function(oMarker, type, ix)
			{
				var index_padded	= padZero(ix, 3);
				
				// Set common icon properties
				oMarker.icon		= new google.maps.MarkerImage();
				oMarker.icon.size	= new google.maps.Size(23, 28);
				oMarker.icon.origin	= new google.maps.Point(0, 0);
				oMarker.icon.anchor	= new google.maps.Point(12, 28);
				
				// Set icon url per type supplied
				switch (type)
				{
					// Store
					case 82 : oMarker.icon.url = path_to_theme+'/assets/css/img/map-icons/store'+index_padded+'.png'; break;
					// Restaurant
					case 83 : oMarker.icon.url = path_to_theme+'/assets/css/img/map-icons/dining'+index_padded+'.png'; break;
					// Hotel
					case 84 : oMarker.icon.url = path_to_theme+'/assets/css/img/map-icons/hotel'+index_padded+'.png'; break;
					// Hotspot
					case 85 : oMarker.icon.url = path_to_theme+'/assets/css/img/map-icons/hotspot'+index_padded+'.png'; break;
					// Other
					case 113 : 
					default : oMarker.icon.url = path_to_theme+'/assets/css/img/map-icons/other'+index_padded+'.png'; break;
				}
				
				// Set common shadow and shape objects
				oMarker.shadow	= new google.maps.MarkerImage(
					path_to_theme+'/assets/css/img/map-icons/_shadow.png',
					new google.maps.Size(41, 28),
					new google.maps.Point(0, 0),
					new google.maps.Point(12, 28)
				);
				
				oMarker.shape	= {
					coord	: [15,0,17,1,18,2,19,3,20,4,20,5,21,6,21,7,22,8,22,9,22,10,22,11,22,12,22,13,22,14,21,15,21,16,21,17,20,18,19,19,19,20,18,21,16,22,14,23,13,24,12,25,11,26,10,26,9,25,8,24,7,23,5,22,4,21,3,20,2,19,1,18,1,17,0,16,0,15,0,14,0,13,0,12,0,11,0,10,0,9,0,8,0,7,0,6,1,5,2,4,2,3,3,2,4,1,6,0,15,0],
					type	: 'poly'
				};
			},		
			tooltip : null,
			tooltipMarkerZindex : 1000,
			openTooltip : function(mapInstance, marker, panorama, tooltipContent) {
				
				var currentLatLng = null;
				if (paintMap.tooltip) {
					currentLatLng = paintMap.tooltip.position;
				}
				if (paintMap.closeTooltip()) {
					if (marker.position == currentLatLng) {
						/*
						 * If incoming marker.position != currentLatLng, then 
						 * user clicked another marker and we should continue 
						 * processing on the new marker; otherwise, just exit.
						 * 
						 * */ 
						return true;
					}
				}
				
				marker.setZIndex( paintMap.tooltipMarkerZindex );
				
				paintMap.tooltip = new google.maps.InfoWindow({
					content			: tooltipContent,
					position		: marker.position,
					zIndex			: paintMap.tooltipMarkerZindex,
					cornerRadius	: 3,
					padding			: 10,
					height			: null
				});
				
				paintMap.tooltip.open(mapInstance, marker);
				
				paintMap.tooltipMarkerZindex++; // Increment global zindex counter to set next max position, since marker icons can be stacked on top of each other
				
				
				// Add handler to close tooltip if user clicks [X]
				google.maps.event.addListener(paintMap.tooltip, 'closeclick', function() {
					paintMap.closeTooltip();
				});
				
				// Show a panorama streetview
				/*
				if (panorama !== null && panorama.length) {
				    var oPanorama = new google.maps.StreetViewPanorama(
				    		panorama[0],
							{
								position : marker.position,
								pov : {
									heading	: 34,
									pitch	: 10,
									zoom	: 1
								}
							}
				    	);
				    
				    panorama.show();
				    mapInstance.setStreetView(oPanorama);
				}
				*/
			},
			closeTooltip : function() {
				if (paintMap.tooltip) {
					// Close instance, reset
					paintMap.tooltip.close();
					paintMap.tooltip = null;
					return true;
				}
				
				return false;
			},
			zoomToFit : function(mapInstance, arrMarkers) {
				if (arrMarkers.length) {
					if (arrMarkers.length == 1) {
						// Single point
						mapInstance.center = arrMarkers[0].position;
						mapInstance.setZoom( 18 );
						mapInstance.panTo( arrMarkers[0].position );
						//mapInstance.panBy(50, -120); // Pan the map by x/y pixels to better center the balloon
					}
					else {
						// Multiple points
						var oBounds = new google.maps.LatLngBounds();
						$.each(arrMarkers, function(ix) {
							oBounds.extend( arrMarkers[ix].position ); // Increase the bounds to include this point
						});
						
						mapInstance.fitBounds(oBounds);
					}
				}
			},
			getRoute : function(mapInstance, arrMarkers) {
				
				var directionsWrapper	= $('#route-directions');
				var directionsMode		= $('#scoutscoop-itinerary-mode-form #edit-mode');
				var google_debug 		= $('#google-debug');
				
				if (directionsWrapper.length
				&&	arrMarkers.length > 1)
				{
					// Set travel mode
					var mode = google.maps.DirectionsTravelMode.DRIVING;
					if (directionsMode.length) {
						switch (directionsMode.val())
						{
						case 'WALKING'		: mode = google.maps.DirectionsTravelMode.WALKING; break;
						case 'BICYCLING'	: mode = google.maps.DirectionsTravelMode.BICYCLING; break;
						case 'DRIVING'		:
						default				: mode = google.maps.DirectionsTravelMode.DRIVING; break;
						}
					}
					
					// Set route stopovers
					var ptOrigin				= null;
					var ptStops					= new Array();
					var strDirectionsRawInput	= '<p>Here is the raw address data that was sent to Google; destination names are shown here for clarity, but are not included in the request. As part of the route optimization, the order of destinations is changed in the response data.</p><ol>';
					$.each(arrMarkers, function(ix) {
						var pt = { location : arrMarkers[ix].custom_data.address_machine, stopover : true };
						
						if (ix==0) {
							ptOrigin = pt;
						}
						else {
							ptStops.push(pt);
						}
						
						strDirectionsRawInput += '<li>'+arrMarkers[ix].title+'<br />'+arrMarkers[ix].custom_data.address_machine + '</li>';
					});
					strDirectionsRawInput += '</ol>';
					
					
					// Get optimized route
					var oRouteService = new google.maps.DirectionsService();
					oRouteService.route({
							origin				: ptOrigin.location, // Circular route, origin = destination
							destination			: ptOrigin.location,
							waypoints			: ptStops,
							optimizeWaypoints	: true, // THIS IS THE KEY TO "OPTIMIZED ROUTING"!!! See http://grabity.blogspot.com/2010/03/route-optimization-using-google-maps.html
							avoidTolls			: true,
							travelMode			: mode
						},
						function(response, status) // Process response
						{
							if (status !== google.maps.DirectionsStatus.OK) {
								//alert('something went wrong');
								return false;
							}
							
							var matchMarkerToLeg = function(oLeg)
							{
								var oMarker			= null;
								var min_distance	= 0;
								
								$.each(arrMarkers, function(ix) {
									/* 
									 * Why we need this ugly distance calculator: 
									 * 
									 * 1) The DirectionsWaypoint object (ptStops) above does not allow any custom fields to be appended,
									 *    so we have no way of associating existing marker (with all it's custom_data fields) to the coordinate.
									 * 
									 * 2) Even if we could, the original DirectionsWaypoint vars do not come through in the response's DirectionsResult object
									 * 
									 * 3) DirectionsResult's route does not contain a 1:1 mapping of Destination => leg...we inevitably have to repeat Destination entries.
									 * 
									 * 4) The distance calculator checks the leg's lat/lng against each marker's lat/lng, then keeps the marker with the lowest distance between points;
									 *    by this method, we are associating marker => result. Rinse and repeat below for the delta return leg records.
									 * 
									 * 5) Using this local calculator is cheaper than using Google's DistanceMatrixService
									 * 
									 * */
									
									var thisMarker	= arrMarkers[ix];
									var thisLatLng	= thisMarker.getPosition();
									var distance	= calculateDistance(
											oLeg.start_location.lat(),
											oLeg.start_location.lng(),
											thisLatLng.lat(),
											thisLatLng.lng()											
										);
									
									if (distance < min_distance || min_distance == 0) {
										min_distance	= distance;
										oMarker			= thisMarker;
									}
								});
								
								// Update marker's postion on map (more accurate for plotting directly on the poly)
								oMarker.setPosition(oLeg.start_location);
								
								return oMarker;
							};
							
							
							/*
							 * Loop optimized waypoint_order + delta to associate index => (leg, marker)
							 * 
							 * */
							
							var route	= new Array();
							var delta	= response.routes[0].legs.slice( response.routes[0].waypoint_order.length );
							
							// Loop optimized waypoints
							$.each(response.routes[0].waypoint_order, function(key1, val1) {
								var oLeg	= response.routes[0].legs[val1];
								var oMarker	= matchMarkerToLeg(oLeg);
								
								route[val1] = {
										leg		: oLeg,
										marker	: oMarker
									};								
							});
							
							// Sort route by numeric index asc
							route.sort(function(a,b){return a - b});
							
							// Loop delta waypoints (return legs)
							$.each(delta, function(ix) {
								var oLeg	= delta[ix];
								var oMarker	= matchMarkerToLeg(oLeg);
								
								route.push({
									leg		: oLeg,
									marker	: oMarker
								});
							});
							
							
							// Paint final html
							directionsWrapper.empty();
							
							var ix = 0;
							$.each(route, function(key, val)
							{
								var is_first	= (ix == 0);
								var is_last		= (ix+1 == route.length);
								var show_leg	= (val.leg.distance.value > 0 && val.leg.duration.value > 0);
								
								if (!show_leg) {
									val.marker.setMap(null);
								}
								
								if (show_leg)
								{
									var index_padded = padZero((ix+1), 3);
									val.marker.icon.url = val.marker.icon.url.substr(0, val.marker.icon.url.length-7) + index_padded + '.png'; 
									
									var leg = 
										'<div class="leg'+ (is_first ? ' first' : '') + (is_last ? ' last' : '') +'">' +
											'<div class="grid-6 alpha">' +
												'<a href="'+val.marker.custom_data.path+'" class="corners title hide-txt" style="background-image:url('+ val.marker.custom_data.image + ');">'+val.marker.title+'</a>' + 
											'</div>' +
											'<div class="grid-18 omega">' +
												'<h4 class="gray corners top clear">' + 
													'<a href="#main-content" class="icon corners hide-txt show-on-map" style="background:url('+val.marker.icon.url+') no-repeat 100% 0;">'+val.marker.title+'</a>' +
													'<a href="/scoutscoop_itinerary/remove_destination/'+val.marker.custom_data.nid+'/'+val.marker.custom_data.nid_child+'/'+val.marker.custom_data.genid+'?destination='+window.location.href+'" class="icon remove hide-txt">'+val.marker.title+'</a>' +
													'<a href="'+val.marker.custom_data.path+'" class="title">'+val.marker.title+'</a>' + 
												'</h4>' +
												'<div class="gray corners bottom clear">' + 
													(val.marker.custom_data.phone.length ? '<div class="phone">' + val.marker.custom_data.phone + '</div>' : '') + 
													(val.marker.custom_data.hours.length ? '<div class="hours">hours: ' + val.marker.custom_data.hours + '</div>' : '') + 											
													 val.marker.custom_data.address_human + //val.leg.start_address
												'</div>';
										leg +='<p>'+val.leg.distance.text+' - about '+val.leg.duration.text+'</p>' +
												'<table>';
												$.each(val.leg.steps, function(s) {
													leg += 
													'<tr>' +
													'<td class="first">'+(s+1)+'.</td>' +
													'<td>'+val.leg.steps[s].instructions+'</td>' + 
													'<td class="right">'+val.leg.steps[s].distance.text+'</td>' +
													'</tr>';
												});
										leg +='</table>';
										leg += 
											'</div>' +
											'<div class="clear"></div>' + 
										'</div>';								
									
									
									leg = $(leg);
									
									// Add handler to open tooltip from external link, zoom map
									var map_links = leg.find('a.show-on-map');
									
									map_links.click(function(evt) {
										paintMap.openTooltip(mapInstance, val.marker, null, val.marker.custom_data.tooltip);
										paintMap.zoomToFit(mapInstance, new Array(val.marker));
									});
									
									map_links.click(smoothScrollAnchors);
									
									
									directionsWrapper.append(leg);
									
									ix++;
								}
							});
							
							
							// Delete confirmations for remove links
							directionsWrapper.find('a.remove').click(function(evt) {
								var self = $(this);
								return confirm('Are you sure you want to\nremove "'+self.text()+'"?');
							});
							
							// Debug stuff so testers can compare custom processing to Google's default implementation
							google_debug.find('.inner').empty();
							google_debug.find('.input').empty();
							google_debug.find('.input').append(strDirectionsRawInput);							
							$('a.toggle-google').unbind('click');
							$('a.toggle-google').click(function(evt) {
								evt.preventDefault();
								google_debug.toggle();
							});
							
							// Render directions poly
							var oRouteRenderer = new google.maps.DirectionsRenderer({
								map					: mapInstance,
								directions			: response,
								panel				: google_debug.find('.inner')[0],
								hideRouteList		: true,									
								preserveViewport	: false,
								suppressMarkers 	: true,
								suppressInfoWindows : true,
								polylineOptions		: {
										map			: mapInstance,
										strokeColor	: '#578cce',
										strokeWeight: 5
									}
							});
							
							
						} // end callback function
					); // end .route call
					
				}
			},
			init : function(target, targetPanorama, locations) {
				
				// Instantiate map
				var oMap = new google.maps.Map(
						target[0],
						{
							center					: new google.maps.LatLng(0, 0),
							mapTypeId				: paintMap.mapTypeId,
							mapTypeControlOptions	: { mapTypeIds: [] }, // Empty array hides buttons [ google.maps.MapTypeId.ROADMAP, paintMap.mapTypeId ]
							zoom					: 18,
			     			zoomControl				: true,
			     			zoomControlOptions 		: { style: google.maps.ZoomControlStyle.SMALL },
			     			panControl				: false,
			     			streetViewControl 		: false
						}
					);
					
				// Apply custom map styles
				oMap.mapTypes.set(
						paintMap.mapTypeId,
						new google.maps.StyledMapType(paintMap.styles, paintMap.styleOptions)
					);
					
				// Loop locations, plot markers and tooltips
				var arrMarkers	= new Array();
				
				$.each(locations, function(ix)
				{
					var location	= locations[ix];
					var oLatLng		= new google.maps.LatLng(location.latitude, location.longitude);
					var oMarker		= new google.maps.Marker({
							position	: oLatLng,
							zIndex		: (ix+1),
							map			: oMap,
							optimized	: true,
							title		: location.title
						});
					
					// Add a custom data object to marker object; this will be used for rendering in getRoute()
					oMarker.set('custom_data', {
						nid					: location.nid,
						nid_child			: location.nid_child,
						genid				: (location.genid ? location.genid : '0'),
						type				: location.type,
						address_human		: location.address_human,
						address_machine		: location.address_machine,
						path				: location.path,
						image				: location.image,
						phone				: location.phone,
						hours				: location.hours,
						tooltip				: location.tooltipContent
					});
					
					paintMap.setIcon(oMarker, location.type, (ix+1));
					
					// Add handler to open tooltip
					google.maps.event.addListener(oMarker, 'click', function() {
						paintMap.openTooltip(oMap, oMarker, targetPanorama, oMarker.custom_data.tooltip);
						paintMap.zoomToFit(oMap, new Array(oMarker));
					});
					
					// Add handler to open tooltip from external link, zoom map
					$('.results a.show-on-map.'+location.markerId).click(function(evt) {
						paintMap.openTooltip(oMap, oMarker, targetPanorama, oMarker.custom_data.tooltip);
						paintMap.zoomToFit(oMap, new Array(oMarker));
					});
					
					// Add marker to collection
					arrMarkers.push(oMarker);
				});
				
				// Zoom to fit all markers
				paintMap.zoomToFit(
						oMap,
						arrMarkers
					);
				
				// Auto-paint optimized route if directions wrapper available
				paintMap.getRoute(
						oMap,
						arrMarkers
					);
				
				return oMap; // Return current map instance
			}
		};
	
	
/*
 * TEMPLATE FUNCTIONS
 * 
 * If you wrote a really cool ELEMENT-SPECIFIC function, OR ANYTHING THAT USES 
 * THE UTILITY STUFF ABOVE, put it here
 * 
 * */
	
	// Apply "last" class to final elements; this is to account for areas that can't easily be altered using Drupal functions
	$('#bd p:last-child, #bd .comments .comment').addClass('last');
	
	// Template text fields to focus/blur default value
	$.each([
	        $('#fs-keyword input.form-text'), 		// Keyword searches
	        $('#user-login-form input.form-text'),	// Login form
	        $('#user-pass input.form-text') 		// Password form
	        ],
	        function(ix) { applyTxtFocusBlur.init($(this)); }
		);
		
	// Main nav hackery
		/*
		$('#hd .super').delay({
			event	: 'closeme',
			delay	: 1000,
			fn		: 
				function(e, obj) {
					console.debug(e, $(obj) );
				}
			});
		*/
		
		// Close the Destinations subnav
			$('#hd .super').bind('closeme', function(evt, parent, suppress) {
				var self = $(this);
				
				if (self.hasClass('active') 
				&&	suppress !== true) {
					self.slideUp('fast', function() {
						parent.append(self);
						parent.removeClass('active');
						self.removeClass('active');
					});
				}
			});
			
		// Mouseout of Destination subnav to close
		// (unless we just mouseentered the parent list item or link)
			$('#hd .super').bind('mouseleave', function(evt) {
				var menu		= $(this);
				var parent		= $('li#nav-destinations');
				var suppress	= false;
				
				if (evt.relatedTarget !== null) {
					if (evt.relatedTarget.id == parent.attr('id')) {
						suppress = true;
					}
					else if (evt.relatedTarget.id == parent.find('a:first').attr('id')) {
						suppress = true;
					}
				}
				
				menu.trigger('closeme', [ parent, suppress ]);
			});
			
		// Hover to show/hide subnav
		// (unless we just mouseentered the subnav)
			$('#nav-destinations').hover(
				function(evt) {
					var self	= $(this);
					var menu	= self.find('.super');
					
					if (menu.length) {
						// menu exists in li; move it to the bottom of #hd
						$('#hd').append(menu);
						menu.slideDown('fast', function() { });
						self.toggleClass('active');
						menu.toggleClass('active');
					}
				},
				function(evt) {
					var menu		= $('#hd .super.active');
					var parent		= $(this);
					var suppress	= false;
					
					if (evt.relatedTarget !== null) {
						if (menu.attr('id') == evt.relatedTarget.id) {
							suppress = true;
						}
					}
					
					menu.trigger('closeme', [ parent, suppress ]);
				}
			);
			
		// Hover to show/hide subnav
			$('#nav-myscout').hover(
				function() {
					var li		= $(this);
					var menu	= li.find('ul.menu');
					var li_prev	= li.prev('li');
					
					if (!menu.hasClass('corners')) {
						menu.addClass('corners bottom');
					}
					
					li.addClass('active');
					li_prev.find('a:first').addClass('transparent');
					menu.show();
					//menu.slideDown('fast', function() { });
				},
				function() {
					var li		= $(this);
					var menu	= li.find('ul.menu');
					var li_prev	= li.prev('li');
					
					li.removeClass('active');
					li_prev.find('a:first').removeClass('transparent');
					menu.hide();
					//menu.slideUp('fast', function() { });
				}
			);
			
	// User profile form (hack for password field corners)
		$('#profile_form_profile input[type="password"]').addClass('corners');
		
	// Keyword search submit
		$('#fs-keyword form').submit(function() {
			var form			= $(this);
			var keyword			= form.find('input[name="keyword"]');
			var keyword_apply	= form.find('input[name="keyword_apply"]:checked');
			var hidden			= form.find('input[type="hidden"]');
			
			// Don't submit default value
			if (keyword.val() == 'enter search terms') { return false; }
			
			// New keyword search; invalidate hidden search params
			if (keyword_apply.val()=='new')
			{
				var exclusions = new Array('contenttype','pagesize');
				
				hidden.each(function(ix) {
					var self	= $(this);
					var key		= self.attr('name');
					var exclude	= jQuery.inArray(key, exclusions);
					
					if (exclude == -1) {
						self.val('0');
					}
				});
			}
		});
		
	// Advanced search submit (GET method)
		$('#fs-advanced form').submit(function() {
			var form	= $(this);
			var input	= form.find('input, select, hidden');
			var pairs	= new Array();
			
			input.each(function(ix) {
				var self	= $(this);
				var type	= self.attr('type');
				var key		= self.attr('name');
				var val		= self.val();
				
				if (key !== '' && val !== '0') {
					switch (type) {
					case 'radio' :
						if (self.is(':checked')) {
							pairs.push(key+'='+val);
						}
						break;
					default :
						pairs.push(key+'='+val);
						break;
					}
				}
			});
			
			window.location.href = form.attr('action')+'?'+pairs.join('&');
			return false;
		});
		
	// Advanced search show/hide
		$('#fs-advanced a.open').click(function(evt) {
			evt.preventDefault();
			var self	= $(this);
			var panel	= self.next('.panel');
			panel.slideDown('fast', function() {});
		});
		
		$('#fs-advanced a.close').click(function(evt) {
			evt.preventDefault();
			var self	= $(this);
			var panel	= self.parents('.panel');
			panel.slideUp('fast', function() {});
		});
		
	// Faceted search show/hide groups
		$('.fs-filters .group h3 a.toggle').click(function(evt) {
			evt.preventDefault();
			
			var self	= $(this);
			var parent	= self.parents('.group');
			var list	= parent.find('ul');
			
			if (list.length) {
				if (list.is(':visible')) {
					list.slideUp('fast', function() {
						parent.toggleClass('active');
					});
				}
				else {
					list.slideDown('fast', function() {
						parent.toggleClass('active');
					});
				}
			}
		});
		
	// User search preferences show/hide
		$('a.open.edit.prefs').click(function(evt) {
			evt.preventDefault();
			
			var self	= $(this);
			var panel_1	= $('#fs-prefs .form-checkboxes.write, #fs-prefs .form-submit.write, #fs-prefs p.write');
			var panel_2	= $('#fs-prefs .read');
			
			panel_1.toggle();
			panel_2.toggle();
			
			// Auto-check all boxes is "All" found in table cell
			panel_2.each(function(ix) {
				var self = $(this);
				if (self.html() == 'All') {
					var td			= self.parent('td');
					var checker		= td.find('a.check');
					var checkboxes	= td.find('.form-checkboxes input[type="checkbox"]');
					
					checker.text('Select None');
					checkboxes.attr('checked', 'checked');
				}
			});
			
			self.text( (panel_1.is(':visible') ? 'Cancel' : 'Edit categories') );
			self.toggleClass('active');
		});
		
	// User search preferences select all/none for checkboxes
		$('#fs-prefs a.check').click(function(evt) {
			evt.preventDefault();
			
			var checker		= $(this);
			var checkboxes	= checker.parents('td').find('.form-checkboxes input[type="checkbox"]');
			var checked		= ((checker.text()=='Select None') ? '' : 'checked');
			
			checker.text( (checked=='' ? 'Select All' : 'Select None') );
			checkboxes.attr('checked', checked);
		});
		
	// Onchange auto-submit for dropdowns		
		$('#views-exposed-form-search-page-1 select').change(function() {
			$(this).parents('form').submit();
		});
		
	// City results header click (city name)
		$('.results-wrapper.city .bar h2 a.toggle').click(function(evt) {
			evt.preventDefault();
			
			var self			= $(this);
			var wrapper_city	= $('#views-exposed-form-search-page-1 #edit-city-wrapper');
			
			if (wrapper_city.is(':visible')) {
				wrapper_city.hide();
				self.css({ 'text-indent' : '0' });
			}
			else {
				wrapper_city.show().find('select').attr('size', 17);
				self.css({ 'text-indent' : '-9999px' });
			}
		});
		
	// Delete confirmations for remove links in a table (e.g. the MyScout page)
		$('table.data a.remove').click(function(evt) {
			var self = $(this);
			return confirm('Are you sure you want to\ndelete "'+self.text()+'"?');
		});
		
	// Itinerary (+ any other) print links
		$('a.edit.print').click(function(evt) {
			evt.preventDefault();
			window.print();
		});
		
	// Itinerary title form show/hide
		$('.itinerary.title a.edit').click(function(evt) {
			evt.preventDefault();
			
			var self	= $(this);
			var h2		= self.parents('h2');
			var view	= h2.find('span.view');
			var edit	= h2.find('span.edit');
			
			view.toggle();
			edit.toggle();
			
			if (edit.is(':visible')) { edit.find('input[type="text"]').focus(); }
		});
		
	// Itinerary destination start add form show/hide
		$('.itinerary.start a.open').click(function(evt) {
			evt.preventDefault();
			
			var self	= $(this);
			var panel	= self.parents('.itinerary.start').find('.panel');

			panel.slideDown('fast', function() { });
		});
		
		$('.itinerary.start a.close, .itinerary.start #scoutscoop-itinerary-start-select-form input.form-submit').click(function(evt) {
			evt.preventDefault();
			
			var self	= $(this);
			var panel	= self.parents('.itinerary.start').find('.panel');
			
			panel.slideUp('fast', function() { });
		});
		
	// Itinerary destination start form add label+blur trick 
		var labels = $('.itinerary.start .panel label');
		if (labels.length) {
			labels.each(function(ix) {
				var label		= $(this);
				var input		= label.parent('.form-item').find('input[type="text"]');
				
				if (input.length && input.val() == '') {
					input.val(label.text());
					applyTxtFocusBlur.init(input);
				}
			});
		}
		
	// Destination + News detail page carousel
		var slideWrapper = $('.carousel > .slides');
		if (slideWrapper.length)
		{
			slideWrapper.find('li a').click(function(evt) {
				evt.preventDefault();
				
				var self	= $(this);				
				var ix		= self.attr('rel');
				var current	= $(self.parents('.carousel').find('.images div.image')[ix]);
				
				var prev	= self.parents('.slides').find('li.active:first a');
				var ix_prev	= prev.attr('rel');
					prev	= $('.carousel .images').find('div.image:visible');
					prev	= $(prev);
					
				prev.fadeTo('normal', 0, function() {
					prev.hide();
					slideWrapper.find('li').removeClass('active');
					self.parent('li').addClass('active');
				});
				
				current.fadeTo('fast', 1, function() {
					current.show();
				});				
			});
			
			if (slideWrapper.parent('.carousel').hasClass('active')) {
				applyCarousel.init(
					slideWrapper,
					$('.carousel a.prev:first'),
					$('.carousel a.next:first')
				);
			}
		}
		
	// Animate anchor links. jQuery SmoothScroll v.11-03-14
		var smoothScrollAnchors = function(evt) {
			var duration	= 1000; // duration in ms
			var easing		= 'swing'; // easing values: swing | linear
			var newHash		= this.hash;
			var target		= $('a[name='+this.hash.slice(1)+']');
			
			if (target.length)
			{
				evt.preventDefault();
				
				target = target.offset().top;
				
				var oldLocation	= window.location.href.replace(window.location.hash, '');
				var newLocation	= this;
				
				// make sure it's the same location
				if(oldLocation+newHash==newLocation) {
					// set selector
					if($.browser.safari) {
						var animationSelector='body:not(:animated)';
					}
					else {
						var animationSelector='html:not(:animated)';
					}
					
					// animate to target and set the hash to the window.location after the animation
					$(animationSelector).animate({ scrollTop: target }, duration, easing, function() {
						// add new hash to the browser location
						window.location.href=newLocation;
					});
				}
				
				return false;
			}
		};
		
		$('a[href*=#]').click(smoothScrollAnchors);
		
		
		
		
	// Google Maps v3 stuff (see paintMap class above for the meaty bits)
	// Maps - static homepage + Google Maps init on search results + itineraries
		var map 		= $('#map-canvas');
		var panorama	= $('#map-street');
		var cache		= 
		{
			maps : {
				gmap			: null,
				wrapper 		: $('.results .map-wrapper'),
				wrapper_close	: $('.results .map-wrapper a.close'),
				wrapper_inner	: $('.results .map-wrapper .inner'),
				areas			: $('.results .map-wrapper area'),
				area_menu		: $('.results .map-wrapper .area-menu'),
				extern_link		: null
			}
		};
		
		var initPaintMap = function ()
		{
			if (map.length 
			&&	locationData.length
			&&	cache.maps.gmap == null
			)
			{
				cache.maps.gmap = paintMap.init(map, panorama, locationData);
				
				if (cache.maps.extern_link !== null) {
					// Trigger click event that was bound in paintMap
					cache.maps.extern_link.trigger('click');
				}
				
				map.show();
			}
		};
		
		
		// Itinerary destination start select form
			$('#scoutscoop-itinerary-start-select-form input.form-submit').click(function(evt) {
				
				if (cache.maps.gmap == null) { return false; }
				
				var self	= $(this);
				var select	= self.parents('form').find('select');
				var value	= select.val();
				
				if (value == '') { return false; }
				
				var currentStartNid	= $('#scoutscoop-itinerary-start-add-form input[name="nid"]');
				var locationDataNew	= new Array();
				var locationDataAdd	= new Array();
				
				$.each(locationData, function(ix) {
					var obj = $(this)[0];
					
					if (obj.markerId == 'marker-'+value) {
						locationDataNew[0] = obj; // Set selected node at index 0
					}
					else if (obj.markerId !== 'marker-'+currentStartNid.val()) {
						locationDataAdd.push(obj); // Add obj to collection if it's != current Itinerary Start Location nid
					}
				});
				
				// Set final collection
				$.each(locationDataAdd, function(ix) {
					var obj = $(this)[0];
					locationDataNew.push(obj);
				});
				
				// Nullify map to force repaint, set updated global locationData var
				cache.maps.gmap	= null;
				locationData	= locationDataNew;
				initPaintMap();
				
				return false;
			});
			
		// Itinerary travel mode select form
			$('#scoutscoop-itinerary-mode-form input.form-submit').click(function(evt) {
				if (cache.maps.gmap == null) { return false; }
				
				var self	= $(this);
				var select	= self.parents('form').find('select');
				var value	= select.val();
				
				if (value == '') { return false; }
				
				
				
				// Nullify map to force repaint
				cache.maps.gmap	= null;
				initPaintMap();
				
				return false;
			});
			
		// Destination detail page
			if ($('#bd.destination').length) {
				initPaintMap();
			}
			
		// Itinerary detail page
			if ($('#bd.itin').length) {
				initPaintMap();
			}		
			
		// Search results link; trigger expand/contract + click
			$('.results a.show-on-map').click(function(evt) {
				cache.maps.wrapper.trigger('click');
				cache.maps.extern_link = $(this);
			});
			
		// Expand/contract animation for the map wrapper
			cache.maps.wrapper.click(function(evt) {
				var wrapper	= $(this);
				var close	= cache.maps.wrapper_close;
				var inner	= cache.maps.wrapper_inner;
				var menu	= cache.maps.area_menu;
				
				if (!wrapper.hasClass('open'))
				{
					wrapper.css({ 'overflow' : 'visible', 'cursor'	: 'auto' });
					inner.css({ 'overflow' : 'visible' });
					
					map.fadeIn(500);
					
					wrapper.animate({
							'width'		: '710px',
							'height'	: '450px'
						},
						500,
						function() {
							close.show();
							wrapper.toggleClass('open');
							initPaintMap();
							
							// Auto-expand the North America menu if present
							if (cache.maps.areas.length) {
								cache.maps.areas.each(function(ix) {
									var self = $(this);
									if (self.hasClass('nav-northamerica')) {
										toggleContinentMenu( self );
									}
								});
							}
							
						}
					);
				}
			});
			// Map wrapper's close button
				cache.maps.wrapper_close.click(function(evt) {
					evt.preventDefault();
					
					var close	= $(this);
					var wrapper	= cache.maps.wrapper;
					var inner	= cache.maps.wrapper_inner;
					var menu	= cache.maps.area_menu;
					
					map.fadeOut(500);
					close.hide();
					menu.hide();
					
					wrapper.css({ 'overflow' : 'hidden', 'cursor' : 'pointer' });
					inner.css({ 'overflow' : 'hidden' });
					
					wrapper.animate({
							'width'		: '230px',
							'height'	: '220px'
						},
						500,
						function() {
							wrapper.toggleClass('open');
						});
				});
			
			
		// Custom homepage map menu (no Google stuffs here)
			var toggleContinentMenu = function(obj) {
				var self		= obj;
				var navclass	= self.attr('class');
				var parent		= cache.maps.wrapper;
				var inner		= cache.maps.wrapper_inner;
				var wrapper		= cache.maps.area_menu;
				
				if (parent.hasClass('open')
				&&	!wrapper.hasClass(navclass))
				{
					// Get continent menu items
					var menu = inner.find('div.'+navclass).clone();
					
					cache.maps.areas.each(function(ix) {
						wrapper.removeClass( $(this).attr('class') );
					});
					
					wrapper.html('');
					wrapper.append( menu );
					menu.fadeIn(500);
					wrapper.show();
					wrapper.addClass(navclass);
				}
				else if (wrapper.hasClass(navclass)) {
					var menu = wrapper.find('div.'+navclass);
					
					menu.fadeIn(500);
					wrapper.show();
				}
			};
			
			cache.maps.areas.hover(
				function() { toggleContinentMenu( $(this) ); },
				function() { }
			);
		
		
});

