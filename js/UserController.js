(function () {
    "use strict";

    angular.module(APPNAME)
        .controller('navController', NavController);

    angular.module(APPNAME)
               .directive('validPasswordC', InputConfirm);

    NavController.$inject = ['$baseController', '$userService'];

    function InputConfirm() {
        return {
            require: 'ngModel',
            link: function (scope, elm, attrs, ctrl) {

                ctrl.$parsers.unshift(matchParser);

                function matchParser(viewValue, $scope) {

                    var noMatch = (viewValue === scope.nav.registerForm.password.$viewValue);

                    ctrl.$setValidity('noMatch', noMatch);

                    return noMatch;
                }
            }
        }
    }

    function NavController(
        $baseController
        , $userService) {

        var vm = this;

        $baseController.merge(vm, $baseController);

        vm.items = null;

        vm.newPerson = null;
        vm.registerForm = null;
        vm.personFormVisible = false;
        vm.showNewPersonErrors = false;

        vm.isLoggedIn = false;
        vm.signedInUser = { displayName: null };        
        vm.user = null;

        vm.checkForUser = _checkForUser;
        vm.logout = _logout;
        vm.onLogoutSuccess = _onLogoutSuccess;
        vm.onLogoutError = _onLogoutSuccess;
        vm.$userService = $userService;
        vm.fireAlert = _fireAlert;

        //register 
        vm.regSuccess = _regSuccess;
        vm.onRegError = _onRegError;
        vm.onGetError = _onGetError;
        vm.onGetSuccess = _onGetSuccess;
        vm.addPerson = _addPerson;
        vm.resetRegistrationForm = _resetRegistrationForm;


        _init();

        function _init() {

            vm.checkForUser();

            vm.$systemEventService.listen("login", _onTestSystemEvent);

            vm.$userService.getRoleId(vm.onGetSuccess, vm.onGetError)

        }

        function _checkForUser() {

            if (!vm.$rapidRents.baseViewModel) {
                vm.$log.log("No user signed in");
                vm.$systemEventService.listen("login", _onTestSystemEvent);
                return;
            }

            vm.isLoggedIn = vm.$rapidRents.baseViewModel.isLoggedIn;

            if (vm.isLoggedIn) {
                vm.$log.log(vm.$rapidRents.baseViewModel.currentUser.userName);

                vm.signedInUser.displayName = vm.$rapidRents.baseViewModel.currentUser.userName;
            }

        }

        function _onTestSystemEvent(e, payload) {

            var event = payload[1];

            console.log("on system event", event);

            vm.isLoggedIn = true;

            vm.signedInUser.displayName = event.message;
        }

        function _logout() {
            vm.$userService.logout(vm.onLogoutSuccess, vm.onLogoutError);
        };

        function _onLogoutSuccess() {
            vm.$log.log('Logout went through');
            vm.fireAlert('success', 'You successfully logged out', 'Goodbye');
            vm.$userService.$window.location.assign('/');

        };

        function _onLogoutError() {
            vm.$log.log('Logout error');
        };

        function _fireAlert(type, message, header) {
            vm.$alertService[type](message, header);
        }

        //register

        function _regSuccess(data) {

            vm.fireAlert('success', 'Please check your email to confirm your account', 'Thank you for registering');

                vm.items = data.items;
            vm.resetRegistrationForm();
        }



        function _addPerson() {

            if (vm.registerForm.$valid) {

                vm.$log.log("data is valid! go save this object -> ", vm.newPerson);
                vm.newPerson.userRole = vm.newPerson.userRole.roleId;
                vm.$userService.register(vm.newPerson, vm.regSuccess, vm.onRegError)

            }
            else {
                vm.fireAlert('error', 'This form was submitted with invalid data. Please try again', 'Error');
            }
        }


        function _onRegError(jqXhr, error) {

            vm.showErrors(jqXhr.responseJSON);
        }

        function _onGetSuccess(data) {
            vm.items = data.items;
        }


        function _onGetError(jqXhr, error) {
            vm.showErrors(jqXhr.responseJSON);

        }

        function _fireAlert(type, message, header) {

            vm.$alertService[type](message, header);
        }

        function _resetRegistrationForm() {
            vm.newPerson = null;
            vm.registerForm.$setPristine();
            vm.registerForm.$setUntouched();
        }


    }

})();

(function () {
    "use strict";

    angular.module(APPNAME)
        .controller('loginController', LoginController);

    LoginController.$inject = ['$scope', '$baseController', '$userService'];

    function LoginController(
        $scope
        , $baseController
        , $userService
        ) {

        var vm = this;
        vm.newLogin = null;
        vm.loginForm = null;
        vm.email = null;
        vm.showLoginErrors = false;
        vm.LoginAttemptFailed = false;
        vm.loginObj = {};

        $baseController.merge(vm, $baseController);

        vm.submitVerification = _submitVerification;
        vm.fireAlert = _fireAlert;
        vm.$userService = $userService;
        vm.$scope = $scope;
        vm.onLoginSuccess = _onLoginSuccess;
        vm.onLoginError = _onLoginError;
        vm.getCurrentUser = _getCurrentUser;
        vm.onSuccess = _onSuccess;
        vm.onError = _onError;
        vm.resetLoginForm = _resetLoginForm;


        vm.notify = vm.$userService.getNotifier($scope);

        function _submitVerification() {
            vm.showLoginErrors = true;
            if (vm.loginForm.$valid) {
                vm.$userService.verify(vm.newLogin, vm.onLoginSuccess, vm.onLoginError);
            }
            else {
                vm.$log.info("Did not enter the correct Format.");
                vm.fireAlert('warning', 'Form input incorrect', 'Form Error');
            }
        }

        function _onLoginSuccess(data) {
            if (data.item) {
                vm.getCurrentUser();
                vm.fireAlert('success', 'You are logged in', 'Login success');
            }
            else {
                vm.resetLoginForm();
            }
        }

        function _getCurrentUser() {
            vm.$userService.getCurrentUser(vm.email, vm.onSuccess, vm.onError)
        }

        function _onLoginError(data, jqXhr, error) {
            vm.$log.log("On Login Error");
        }

        function _onSuccess(data, status, xhr) {

            vm.notify(function () {
                vm.loginObj.userName = data.item.userName;
                var eventName = "login";
                var eventPayload = { message: vm.loginObj.userName };
                vm.$systemEventService.broadcast(eventName, eventPayload);
            });
        }


        function _onError(jqXhr, error) {
            vm.$log.log("Error getting email");
        }

        function _fireAlert(type, message, header) {
            vm.$alertService[type](message, header);
        }

        function _resetLoginForm() {
            vm.notify(function () {
                vm.LoginAttemptFailed = true;
                vm.fireAlert('error', 'The Email/Password Combination is incorrect', 'Login Error');
                vm.loginForm.$setPristine();
                vm.newLogin = null;
                vm.loginForm.$setUntouched();
            })
        }
    }
})();