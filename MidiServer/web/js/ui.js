/**
 *
 * User Interface Functions
 *
 * (C) Roland Schabenberger
 *
 */
 
var userinterface = new function() {

	this.initialize = function () {
	
		this.initializeElements();
		
	}
	
	this.initializeDataRange = function (element) {
	
		var binding = element.data(DataBinding.AttributeBinding);
		if (undefined != binding) {
			var source = binding.source;
			if (source == MIDI.Event.PitchBend) {
			
				element.slider( "option", "min", -128 );
				element.slider( "option", "max", 127 );
			
			}
		
		}
	
	}

	this.initializeElements = function () {
	
		$( ".horizontalSlider" ).each(function() {
			var value = parseInt( $( this ).text(), 0 );
			if (null == value || undefined == value || isNaN(value)) value = 0;
			$( this ).text("");
			
			$( this ).slider({
				value: value,
				min: 0,
				max: 127,
				range: "min",
				animate: true,
				orientation: "horizontal"
			});
			
			userinterface.initializeDataRange($(this));
			
		});
		
		$( ".verticalSlider" ).each(function() {
			var value = parseInt( $( this ).text(), 0 );
			if (null == value || undefined == value || isNaN(value)) value = 0;
			$( this ).text("");
			
			$( this ).slider({
				value: value,
				min: 0,
				max: 127,
				range: "min",
				animate: true,
				orientation: "vertical"
			});
			
			userinterface.initializeDataRange($(this));
			
		});

		$( ".spinner" ).each(function() {
			$( this ).spinner({
				step: 1,
				numberFormat: "n"
			});
		});

	}
}