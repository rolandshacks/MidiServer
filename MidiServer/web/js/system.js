/**
 *
 * System Functions
 *
 * (C) Roland Schabenberger
 *
 */

var system = new function() {

	this.timer = null;
	this.timerFunction = null;
	
	this.timeOffset = -1;
	this.timerInterval = 0;
	
	
	this.initialize = function () {
	
	}
	
	this.log = function(text) {
		console.log(text);
	}
	
	this.getTime = function() {
	
		var currentTime = new Date().getTime();
		if (-1 == this.timeOffset) {
			this.timeOffset = currentTime;
			return Number(0);
		} else {
			return Number(currentTime - this.timeOffset);
		}
		
	}
	
	this.timerHandler = function() {
	
		this.timer = null;
	
		if (null != this.timerFunction) {
		
			this.timerFunction();
		
		}
	
	}

	this.setTimer = function(timerInterval, timerFunction) {

		this.stopTimer();
		
		this.timerInterval = timerInterval;
		this.timerFunction = timerFunction;

		timer = window.setInterval("system.timerHandler()", this.timerInterval);
	}
	
	this.stopTimer = function() {
		if (null != this.timer) {
			window.clearInterval(this.timer);
			this.timer = null;
			this.timerFunction = null;
			this.timerInterval = 0;
			
		}
	}
	
}
