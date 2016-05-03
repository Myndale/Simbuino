$(function () {

	// subclassing object, see http://bililite.com/blog/extending-jquery-ui-widgets/
	var object = (function () {
		function F() { }
		return (function (o) {
			F.prototype = o;
			return new F();
		});
	})();


	// generic class for OOP
	Function.prototype.inheritsFrom = function (parentClassOrObject) {
		if (parentClassOrObject.constructor == Function) {
			//Normal Inheritance
			this.prototype = new parentClassOrObject;
			this.prototype.constructor = this;
			this.prototype.parent = parentClassOrObject.prototype;
		}
		else {
			//Pure Virtual Inheritance
			this.prototype = parentClassOrObject;
			this.prototype.constructor = this;
			this.prototype.parent = parentClassOrObject;
		}
		return this;
	}

	Class = {}
	Class.create = function () {

		// parse the input arguments
		var base = Object;
		var members = null
		var index = 0;
		if (typeof (arguments[index]) == "function")
			base = arguments[index++];
		if (typeof (arguments[index]) == "object")
			members = arguments[index++];
		base = base || new function () { }
		members = members || {}

		function newClass() { }

		newClass.inheritsFrom(base);
		newClass.base = base.prototype;
		newClass.prototype.isA = function (classType) {
			return this instanceof classType;
		}

		newClass.prototype._ctor = function (instance, args) {
			if (typeof (members.explicit_ctor) == "function")
				members.explicit_ctor.apply(instance, args);
			else {
				if (base.prototype && base.prototype._ctor)
					base.prototype._ctor(instance, args);
				if (typeof (members.ctor) == "function")
					members.ctor.apply(instance, args);
			}
		}

		newClass.create = function () {
			var result = new newClass();
			newClass.prototype._ctor(result, arguments);
			newClass.prototype.type = newClass;
			return result;
		}

		$.each(members, function (key, value) {
			newClass.prototype[key] = value;
		});

		return newClass;
	}

	// base object class, all classes ultimately derive from this

	var Object = Class.create({
		name: 'Object',

		super: function (base, func) {
			base.prototype[func].call(this);
		}

	});

})
