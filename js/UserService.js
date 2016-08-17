(function () {
    "use strict"

    angular.module(APPNAME)
         .factory('$userService', UserService);

    UserService.$inject = ['$baseService'];

    function UserService($baseService) {
        var arapidRentsServiceObject = rapidRents.services.users;

        var newService = $baseService.merge(true, {}, arapidRentsServiceObject, $baseService);

        return newService;
    };
})();
