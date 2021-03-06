
var liveresApp = angular.module("liveresApp", ['ngRoute', 'liveresControllers','pascalprecht.translate','ngSanitize'])
    .config(['$routeProvider', ($routeprovider: ng.route.IRouteProvider) => {
    $routeprovider.when('/:lang', { templateUrl: 'Views/home.html', controller: 'HomeController' });
    $routeprovider.when('/:lang/comp/:competition/:className?', { templateUrl: 'Views/competition.html', controller: 'CompetitionController', reloadOnSearch: false });
    $routeprovider.otherwise({ redirectTo: '/se' });
}]);


angular.module('liveresControllers', ['LiveResults.Config','pascalprecht.translate'])
        .controller("CompetitionController", <any>LiveResults.Competition.CompetitionController)
        .controller("HomeController", <any>LiveResults.Index.HomeController)
        .controller("AppServices", <any>LiveResults.App.AppServices);

liveresApp.filter('resultTime', [
    () => (time: number, status: number, showHours: boolean, padZeros: boolean) => {
        return LiveResults.Utils.TimeUtils.formatTime(time, status, showHours, padZeros);
    }
]);


var config = angular.module('LiveResults.Config', [])
    .constant('APP_NAME', 'EmmaClient LiveResults')
    .constant('APP_VERSION', '0.2')
    .constant('apiUrl', '/api.php');
