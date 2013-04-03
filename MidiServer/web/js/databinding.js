/**
 *
 * Data Binding Functions
 *
 * (C) Roland Schabenberger
 *
 */

var DataBinding = new function() {

	this.AttributeSource = "source";
	this.AttributeBinding = "binding";
}
 
var databinding = new function() {

	this.bindings = new Array();
	
	this.initialize = function () {
	
		this.initializeBindings();
		
	}

	this.initializeBindings = function () {

		var bindingElements = $("[data-" + DataBinding.AttributeSource + "]");
		
		bindingElements.each(function() {
		
			var id = $(this).attr("id");
			
			var dataSource = $(this).data(DataBinding.AttributeSource);
		
			if (undefined != dataSource) {
			
				var sourceType = typeof(dataSource);
				
				if (sourceType == "object") {
				
					var binding = new ElementBinding($(this), dataSource.controller, dataSource.channel);
					databinding.bindings.push(binding);
				
				} else { // string or number

					var binding = new ElementBinding($(this), dataSource);
					databinding.bindings.push(binding);
				}
			
				//system.log("FOUND BINDING ELEMENT: " +  id + " / " + dataSource + " / " + dataSource.controller);
				
			}
			
		});
	}

	this.bind = function (element, channel, controller) {

		var binding = new ElementBinding(element, channel, controller);
		
		this.bindings.push(binding);
	}

	this.updateBinding = function (binding, channel, source, value) {
		
		if (binding.source != source) {
			return;
		}
		
		if (undefined != binding.channel && binding.channel != channel) {		
			return;
		}
		
		var element = binding.element;
		var domElement = element[0];
		
		if (element.is(".ui-slider")) {
			element.slider( "option", "value", value );
		} else if (element.is(".ui-spinner-input")) {
			element.val( value );
		} else {
			element[0].value = value;
			//system.log("XXXXXX" + element.attr('class'));
		}

	}

	this.dispatch = function(channel, source, value) {

		for (var i=0; i<this.bindings.length; i++) {
			this.updateBinding(this.bindings[i], channel, source, value);
		}

	}
	
}

// class ElementBinding
function ElementBinding(element, source, channel) {

	if (typeof(element) == "string") {
		this.id = element;
		this.element = $("#" + this.id);
	} else {
		this.element = element;
		this.id = element.attr("id");
	}
	
	this.source = parseInt(source);
	if (isNaN(this.source)) {
		this.source = parseInt(eval(source));
		if (isNaN(this.source) || null == this.source || undefined == this.source) {
			system.log("INVALID SOURCE: " + source);
		}
	}
	
	this.channel = channel;
	if (this.channel == undefined || this.channel == null) {
		this.channel = undefined; // do not filter by channel
	} else if (channel < 0) {
		this.channel = undefined; // also channel == -1 for disable
	}
	
	this.element.data(DataBinding.AttributeBinding, this);

}
