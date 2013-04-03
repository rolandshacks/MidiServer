/**
 *
 * Midi Server Java Script Bindings
 *
 * (C) Roland Schabenberger
 *
 */

var MIDI = new function() {

	this.Event = new function() {
		this.NoteOn = 0x90;
		this.NoteOff = 0x80;
		this.PolyphonPressure = 0xA0;
		this.Controller = 0xB0;
		this.ProgChange = 0xC0;
		this.ChannelPressure = 0xD0;
		this.PitchBend = 0xE0;
		this.SystemMessage = 0xF0;
	}
	
	this.Control = new function() {
		this.BankSelectMSB = 0x00;
		this.BankSelectLSB = 0x20;
		this.Modulation = 0x01;
		this.Volume = 0x07;
		this.Pan = 0x0A;
		this.Sustain = 0x40;
		this.Reverb = 0x5B;
		this.Chorus = 0x5D;
		this.AllNotesOff = 0x7B;
	}
	
	this.System = new function() {
		this.TimingClock = 0xF8;
		this.ActiveSensing = 0xFE;
		this.SongPositionPointer = 0xF2;
		this.SongSelect = 0xF3;
		this.Start = 0xFA;
		this.Continue = 0xFB;
		this.Stop = 0xFC;
	}
}

var midi = new function() {

	this.lastBankSelectMSB = -1;
	this.lastBankSelectLSB = -1;
	this.midiChannel = 0;

	this.initialize = function () {
		this.clearMessages();
	}
	
	this.setChannel = function(channel) {
		this.midiChannel = channel;
	}
	
	this.getChannel = function() {
		return this.midiChannel;
	}
	
	this.formatHex = function(value) {
		return ("00" + value.toString(16)).substr(-2).toUpperCase();
	}
	
	this.send = function(byte0, byte1, byte2, byte3) {
		if (null == byte1 || undefined == byte1) byte1 = 0x0;
		if (null == byte2 || undefined == byte2) byte2 = 0x0;
		if (null == byte3 || undefined == byte3) byte3 = 0x0;
		
		//alert("midi: " + this.formatHex(byte0) + "/" + this.formatHex(byte1) + "/" + this.formatHex(byte2) + "/" + this.formatHex(byte3));
		
		var midiData = this.formatHex(byte0) + this.formatHex(byte1) + this.formatHex(byte2) + this.formatHex(byte3);
		
		var response = rpc.postRequest("send", midiData);
		if (null == response) {
			return false;
		}
		
		return ("ok" == response);
	}
	
	this.dispatch = function(byte0, byte1, byte2, byte3) {
	
		if (null != this.midiListener) {
			var result = this.midiListener(byte0, byte1, byte2, byte3);
			if (null != result && (result < 0 || result == false)) {
				return;
			}
		}
		
		var messageId = (byte0 & 0xf0);
		
		if (messageId == MIDI.Event.SystemMessage) {
		
			this.lastBankSelectMSB = this.lastBankSelectLSB = -1;
			
			var message = byte1;

			if (MIDI.System.TimingClock == message)
			{
				if (null != this.onSystemClock) {
					this.onSystemClock();
				}
			}
			else if (MIDI.System.ActiveSensing == message)
			{
				if (null != this.onSystemActiveSensing) {
					this.onSystemActiveSensing();
				}			
			}
			else if (MIDI.System.Start == message)
			{
				if (null != this.onSystemStart) {
					this.onSystemStart();
				}			
			}
			else if (MIDI.System.Continue == message)
			{
				if (null != this.onSystemContinue) {
					this.onSystemContinue();
				}			
			}
			else if (MIDI.System.Stop == message)
			{
				if (null != this.onSystemStop) {
					this.onSystemStop();
				}			
			}
			else if (MIDI.System.SongPositionPointer == message)
			{
				var songPosition = byte1 + byte2 * 128;
				if (null != this.onSystemSongPointer) {
					this.onSystemSongPointer(songPosition);
				}			
			}
			else if (MIDI.System.SongSelect == message)
			{
				var songNumber = byte1;
				if (null != this.onSystemSongSelect) {
					this.onSystemSongSelect(songNumber);
				}			
			}
			else
			{
				;
			}

		} else {
		
			var message = (byte0 & 0xf0);
			var channel = (byte0 & 0x0f);
			
			if (this.midiChannel != undefined && channel != this.midiChannel) {
				return;
			}

			
			if (message != MIDI.Event.Controller &&
				message != MIDI.Event.ProgChange)
			{
				this.lastBankSelectMSB = this.lastBankSelectLSB = -1;
			}

			switch (message)
			{
				case MIDI.Event.PolyphonPressure:
				{
					if (null != this.onPolyphonPressure) {
						this.onNoteOn(channel, byte1, byte2);
					}
					break;
				}
				case MIDI.Event.NoteOn:
				{
					if (0x0 != byte2)
					{
						if (null != this.onNoteOn) {
							this.onNoteOn(channel, byte1, byte2);
						}
					}
					else
					{
						if (null != this.onNoteOff) {
							this.onNoteOff(channel, byte1);
						}
					}

					break;
				}
				case MIDI.Event.NoteOff:
				{
					if (null != this.onNoteOff) {
						this.onNoteOff(channel, byte1);
					}
					break;
				}
				case MIDI.Event.ChannelPressure:
				{
					if (null != this.onChannelPressure) {
						this.onChannelPressure(channel, byte1);
					}
					break;
				}
				case MIDI.Event.ProgChange:
				{
					if (null != this.onProgramChange) {
						this.onProgramChange(channel, byte1);
					}

					this.lastBankSelectMSB = this.lastBankSelectLSB = -1;

					break;
				}
				case MIDI.Event.PitchBend:
				{
					var intValue = (byte1) + (byte2 - 0x40) * 128;

					// range: -128..127
					var value = (intValue >> 6);
					
					// range: -1..1
					//var value = (intValue >= 0) ? intValue / 8191.0 : intValue / 8192.0;
					
					if (null != this.onPitchBend) {
						this.onPitchBend(channel, value);
					}
					
					break;
				}
				case MIDI.Event.Controller:
				{
					var controller = byte1;
					var value = byte2;

					// filter bank select
					if (MIDI.Control.BankSelectMSB == controller)
					{
						this.lastBankSelectMSB = value;
					}
					else if (MIDI.Control.BankSelectLSB == controller)
					{
						this.lastBankSelectLSB = value;
					}
					else
					{
						this.lastBankSelectMSB = this.lastBankSelectLSB = -1;

						// standard control change
						if (null != this.onControlChange) {
							this.onControlChange(channel, controller, value);
						}
					}
					
					break;
				}
				default:
				{
					break;
				}
			}
		}
	}
	
	this.clearMessages = function() {
		rpc.postRequest("clearData");
	}
	
	this.isOpen = function() {
		var response = rpc.postRequest("getStatus");
		if (null == response) {
			return false;
		}
		
		return ("ok" == response);
	}
	
	this.awaitingResponse = false;
	
	this.decode = function(msg) {

		midi.awaitingResponse = false;
	
		if (undefined == msg || null == msg) {
			return;
		}
	
		var numElements = msg.length / 8;
	
		var idx = 0;
		for (var i=0; i<numElements; i++)
		{
			var byte3 = parseInt( msg.substr(idx, 2), 16 ); idx += 2;
			var byte2 = parseInt( msg.substr(idx, 2), 16 ); idx += 2;
			var byte1 = parseInt( msg.substr(idx, 2), 16 ); idx += 2;
			var byte0 = parseInt( msg.substr(idx, 2), 16 ); idx += 2;
			
			midi.dispatch(byte0, byte1, byte2, byte3);
		}
		
	}

	this.processMessages = function() {
	
		if (true == this.awaitingResponse) {
			return;
		}
	
		rpc.postAsyncRequest("getData", null, this.decode);
		
		midi.awaitingResponse = true;
	
	}
	
}
