(function () {
    "use strict";


    angular.module(APPNAME)
    .controller('userController', UserController);

    UserController.$inject = ['$scope', '$baseController', '$userService', '$uibModal'];

    function UserController(
      $scope
    , $baseController
    , $userService
    , $uibModal
    ) {

        var vm = this;
        vm.currentUser = null;
        vm.userName = null;
        vm.modal = null;

        vm.$userService = $userService;
        vm.$scope = $scope;
        vm.$uibModal = $uibModal;
        vm.editUser = _editUser;
        vm.onEditProfileSuccess = _onEditProfileSuccess;
        vm.logout = _logout;
        vm.onLogoutSuccess = _onLogoutSuccess;
        vm.onLogoutError = _onLogoutError;
        vm.onCurrentUserSuccess = _onCurrentUserSuccess;
        vm.onCurrentUserError = _onCurrentUserError;
        vm.openEditProfileModal = _openEditProfileModal;
        vm.editUserPicture = _editUserPicture;
        vm.filePath = null;
        $baseController.merge(vm, $baseController);
        vm.notify = vm.$userService.getNotifier($scope);
        vm.editProfilePictureSuccess = _editProfilePictureSuccess;
        vm.filePath = null;
        vm.fireAlert = _fireAlert;


        function _fireAlert(type, message, header) {
            vm.$alertService[type](message, header);
        }

        _render();

        function _render() {
            vm.userName = vm.$rapidrents.serverModel.currentUser.userName;
            vm.$userService.current(vm.onCurrentUserSuccess, vm.onCurrentUserError);
        }

        function _editUserPicture() {
            if (vm.currentUser) {
                var modalInstance = vm.$uibModal.open({
                    animation: true,
                    templateUrl: 'modalProfilePicture.html',
                    controller: 'editProfilePictureModalController as mc',
                    size: 'md',
                });
                modalInstance.result.then(vm.editProfilePictureSuccess);
            } else {
                vm.fireAlert('error', "Unable to update photo until Profile has been created. Please Edit Profile before Updating Photo.", 'Photo Error');
            }
        };

        function _editProfilePictureSuccess(filePath) {
            vm.filePath = filePath;
        }

        function _editUser() {
            vm.openEditProfileModal(vm.currentUser);
        }

        function _onCurrentUserError() {
            vm.$log.error("There was an error");
        }

        function _onCurrentUserSuccess(data) {
            vm.currentUser = data.item;
            rapidrents.page.user = vm.currentUser;
            if (vm.currentUser == null) {
                vm.filePath = "C17/60827670_blank-profile-picture-973460_960_720.png";
            }
            else {
                vm.filePath = vm.currentUser.avatarFilePath;
            }
            vm.notify(function () {
                vm.filePath;
            })
        }


        function _openEditProfileModal(currentUser) {
            var modalInstance = vm.$uibModal.open({
                animation: true,
                templateUrl: 'modalUser.html',
                controller: 'editProfileModalController as mc',
                size: 'md',
                resolve: {
                    profile: function () {
                        return currentUser;
                    }
                }
            });
            modalInstance.result.then(_onEditProfileSuccess, _cancel);
        };

        function _onEditProfileSuccess(user) {
            vm.currentUser = $baseController.merge(true, vm.currentUser, user);
            vm.$log.log('this is successful');
        };

        function _cancel() {
            vm.profile = null;
            vm.$log.log("Cancelled");
        };

        function _logout() {
            vm.$userService.logout(vm.onLogoutSuccess, vm.onLogoutError);
        };

        function _onLogoutSuccess() {
            vm.$userService.$window.location.assign('/');
        };

        function _onLogoutError() {
            vm.$log.log('there was an error');
        };
    }
})();


(function () {
    "use strict";

    angular.module(APPNAME)
    .controller('editProfilePictureModalController', EditProfilePictureModalController);

    EditProfilePictureModalController.$inject = ['$scope', '$baseController', '$uibModalInstance', '$filesService']

    function EditProfilePictureModalController($scope, $baseController, $uibModalInstance, $filesService) {
        var mc = this;

        mc.$filesService = $filesService;
        mc.$scope = $scope;
        mc.cancel = _cancel;
        $baseController.merge(mc, $baseController);
        mc.$uibModalInstance = $uibModalInstance;
        mc.upload = _upload;
        mc.onUploadPhotoError = _onUploadPhotoError;
        mc.onUploadPhotoSuccess = _onUploadPhotoSuccess;
        mc.notify = mc.$filesService.getNotifier($scope);
        mc.currentFile = null;
        mc.$scope.setFile = _setFile; // Must use $scope because on onchange="angular.element(this).scope().setFile(this)" 

        function _setFile(element) {
            mc.currentFile = element.files[0];
            var reader = new FileReader();

            reader.onload = _onload;

            function _onload(event) {
                mc.notify(function () {
                    mc.image_source = event.target.result;
                });
            }
            reader.readAsDataURL(element.files[0]);
        }

        function _upload() {
            var files = angular.element(document.querySelector('#uploadPhoto'))[0].files;
            var data = new FormData();
            var i;
            for (i = 0; i < files.length; i++) {
                data.append("file", files[i]);
            }
            mc.$filesService.uploadProfilePhoto(data, mc.onUploadPhotoSuccess, mc.onUploadPhotoError);
        };

        function _onUploadPhotoError() {
            mc.$uibModalInstance.dismiss();
        };

        function _onUploadPhotoSuccess(data) {
            mc.$uibModalInstance.close(data.item);
        };

        function _cancel() {
            mc.$uibModalInstance.dismiss();
        };
    };
})();

(function () {
    "use strict";

    angular.module(APPNAME)
    .controller('editProfileModalController', EditProfileModalController);

    EditProfileModalController.$inject = ['$scope', '$baseController', '$uibModalInstance', 'profile', '$userService']

    function EditProfileModalController($scope, $baseController, $uibModalInstance, profile, $userService) {
        var mc = this;

        mc.ok = _ok;
        mc.cancel = _cancel;
        mc.$scope = $scope;
        $baseController.merge(mc, $baseController);
        mc.$uibModalInstance = $uibModalInstance;
        mc.user = profile;
        mc.onUpdateSuccess = _onUpdateSuccess;
        mc.onUpdateError = _onUpdateError;
        mc.$userService = $userService;
        mc.fireAlert = _fireAlert;
        mc.onInsertSuccess = _onInsertSuccess;
        mc.onInsertError = _onInsertError;

        function _fireAlert(type, message, header) {
            mc.$alertService[type](message, header);
        }

        function _ok() {
            mc.user.dateOfBirth = moment(mc.user.dateOfBirth).format();
            if (mc.user.id) {
                mc.$userService.update(mc.user.id, mc.user, mc.onUpdateSuccess, mc.onUpdateError);
            } else {
                mc.user.userId = rapidrents.page.currentUserId;
                mc.$userService.addlegacy(mc.user, mc.onInsertSuccess, mc.onInsertError);
            }
        };

        function _onInsertSuccess(data) {
            mc.user.id = data.item;
            mc.$uibModalInstance.close(mc.user);
            mc.fireAlert('success', "Your Profile has been Inserted", 'Insert Success');
        }
        function _onInsertError() {
            mc.fireAlert('error', "Your Profile has not been Insert", 'Insert Failure');
        }

        function _onUpdateSuccess() {
            mc.fireAlert('success', "Your Profile has been updated", 'Update Success');
            mc.$uibModalInstance.close(mc.user);
        };

        function _onUpdateError() {
            mc.fireAlert('error', "Your Profile has not been updated", 'Update Failure');
        };

        function _cancel() {
            mc.editProfileform.$setPristine();
            mc.editProfileform.$setUntouched();
            mc.$uibModalInstance.dismiss();
        };
    };
})();


(function () {
    "use strict";

    angular.module(APPNAME)
    .directive('convertToNumber', ConvertToNumber);

    function ConvertToNumber() {
        return {
            require: 'ngModel',
            link: function (scope, element, attrs, ngModel) {
                ngModel.$parsers.push(function (val) {
                    return parseInt(val, 10);
                });
                ngModel.$formatters.push(function (val) {
                    return '' + val;
                });
            }
        };
    }
})();


