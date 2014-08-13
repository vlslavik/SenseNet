// using $skin/scripts/sn/SN.js
// using $skin/scripts/ODataManager.js
// using $skin/scripts/OverlayManager.js
// resource BackgroundOperations

var eventTypes = {
    13013: SN.Resources.BackgroundOperations['GeneralError'],
    13014: SN.Resources.BackgroundOperations['CommunicationError'],
    13015: SN.Resources.BackgroundOperations['ExecutionError'],
    13016: SN.Resources.BackgroundOperations['TaskError'],
    13017: SN.Resources.BackgroundOperations['ExecutorTerminated'],
    13018: SN.Resources.BackgroundOperations['ConnectionSlow'],
    13019: SN.Resources.BackgroundOperations['AgentDisconnected'],
    13021: SN.Resources.BackgroundOperations['AgentInitialized'],
    13022: SN.Resources.BackgroundOperations['AgentConnected'],
    13023: SN.Resources.BackgroundOperations['AgentReconnected'],
    13024: SN.Resources.BackgroundOperations['ExecutionStarted'],
    13025: SN.Resources.BackgroundOperations['ExecutionFinished']
};
var eventLevels = ["Verbose", "Information", "Warning", "Error"];
var cpuArray, ramArray, tempCPUArray, tempRAMArray, cpuChart, $cpuValueDiv, ramChart, $ramValueDiv, totalRAM;
var startData = [{
    color: '#2E7199',
    value: 0
}, {
    color: '#36AFE8',
    value: 0
}];
$(function () {
    var hub = $.connection.taskMonitorHub;
    cpuArray = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    ramArray = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    $loading = $('<div class="loading">' + SN.Resources.BackgroundOperations["workingProgress"] + '</div>');
    $('.backgroundOperations').append($loading);


    hub.client.heartbeat = function (agentName, healthRecord) {
        work(agentName, healthRecord);
    };
    hub.client.onEvent = function (data) {
        onEvent(data);
    }
    $.connection.hub.start().done(function () {
        $('#connectionState').removeClass('disconnected').addClass('connected');
        $('#connectionState').attr('title', SN.Resources.BackgroundOperations['Connected']);
        $loading.show();
    });
    setInterval(checkConnections, 5000);
});
function htmlEncode(value) {
    var encodedValue = $('<div />').text(value).html();
    return encodedValue;
}

//--------------------------------------------------------
function work(agentName, healthRecord) {
    var r = JSON.parse(healthRecord);
    var machineName = r.MachineName;
    var taskId = r.TaskId;
    var activity = r.Progress; 
    activity = activity.charAt(0).toUpperCase() + activity.slice(1);
    var cpu = parseFloat(r.CPU);
    var ram = r.RAM;
    var taskType = r.TaskType
    var agent = ensureAgent(machineName, agentName);
    var log = ensureLog(agentName);
    $currentContainer = $('#' + machineName);
    log.lastTime = new Date();
    totalRAM = r.TotalRAM;
    cpuArray[cpuArray.length - 1] = cpu;
    $cpuValueDiv = $currentContainer.find('.cpu .cpu-value');
    $cpuValueDiv.text(cpu + '%');
    ramArray[ramArray.length - 1] = ram;
    $ramValueDiv = $currentContainer.find('.ram .ram-value')
    $ramValueDiv.text(ram + 'MB');

    var cpuSparkline = $currentContainer.find(".cpu .cpu-chart").data("kendoSparkline");
    var ramSparkline = $currentContainer.find(".ram .ram-chart").data("kendoSparkline");

    if (cpuSparkline !== null) {
        cpuSparkline.refresh();
    }
    if (ramSparkline !== null) {
        ramSparkline.refresh();
    }

    var activityClass = activity.toLowerCase() == "idle" ? "idle" : "work";
    setAgentMode(agent, activityClass);
    var activityHtml = activityClass == "idle" ? activity : "TaskId: " + taskId + "<br/>" + activity + "<br/>" + taskType;

    if (activityClass === 'idle') {
        agent.children('.percent').remove();
        agent.children('.info').text('');
        agent.children('.progress').hide();
        agent.children('.icon').show();
    }
    else {
        agent.children('.info').html("TaskId: " + taskId + "<br/>" + activity + "<br/>" + taskType);
        if (activity.indexOf('%') > -1) {
            agent.children('.info').html("TaskId: " + taskId + "<br/>" + taskType);
            agent.append('<div class="percent">' + activity + '</div>')
            agent.children('.icon').hide();
            agent.children('.progress').show();
            var pie = agent.find('.progress').data("kendoChart");
            var s, t;
            s = parseFloat(activity.slice(0, -1));
            t = 100 - s;
            startData = [{
                color: '#2E7199',
                value: s
            }, {
                color: '#36AFE8',
                value: t
            }];
            pie.options.series[0].data = startData;
            pie.refresh();
        }
    }
}
function setAgentMode(agent, mode) { //(agent, "work"|"idle|disconnected")
    agent.removeClass("disconnected, idle, work");
    agent.addClass(mode).attr('title', mode);
}
function ensureAgent(machineName, agentName) {
    $loading.hide();
    machine = $("#" + machineName);
    if (machine.length == 0)
        machine = registerMachine(machineName);
    agent = $("#" + agentName);
    if (agent.length == 0)
        agent = registerAgent(machineName, agentName);
    return agent;
}
function registerMachine(machineName) {
    $machine = $("<div id='" + machineName + "' class='machine'><div class='machinehead'><div class='name'>" + machineName + "</div><div class='cpu'><div>CPU</div><div class='cpu-chart'></div><div class='cpu-value'></div></div><div class='ram'><div>RAM</div><div class='ram-chart'></div><div class='ram-value'></div></div></div><div class='agents'></div></div>");
    $("#machines").append($machine);
    $cpuDiv = $machine.find('.cpu-chart');
    $ramDiv = $machine.find('.ram-chart');

    $cpuDiv.kendoSparkline({
        series: [{
            type: "area",
            data: cpuArray,
            color: "#2E7199"
        }],
        tooltip: {
            format: '{0}%',
            visible: true
        }
    });

    $ramDiv.kendoSparkline({
        series: [{
            type: "column",
            data: ramArray,
            color: "#1EBBA6",
            max: totalRAM
        }],
        tooltip: {
            format: '{0}MB',
            visible: true
        }
    });

    $cpuValueDiv = $('.cpu .cpu-value');
    $cpuValueDiv.text('0 %');

    $ramValueDiv = $('.ram .ram-value');
    $ramValueDiv.text('0 MB');


    result = $("#" + machineName);
    result.click(function () { /*alert(this.innerHTML);*/ })
    return result;
}
function registerAgent(machineName, agentName) {
    $("#" + machineName + ' .agents').append("<div id='" + agentName + "' class='agent'><div class='icon'></div><div class='progress'></div><div class='info'></div></div>");
    $("#" + agentName).find('.progress').kendoChart({
        transitions: false,
        series: [{
            overlay: {
                gradient: "none"
            },
            type: "pie",
            data: startData,
            tooltip: {
                visible: false
            },
            padding: 0
        }],
        chartArea: {
            margin: 1,
            width: 120,
            height: 120,
            background: 'transparent'
        },
        plotArea: {
            margin: 1,
            width: 120,
            height: 120
        }
    });
    result = $("#" + agentName);
    result.click(function () { viewAgentLog(this); })
    return result;
}

//-------------------------------------------------------- logging

maxLogLength = 10;
logs = {};

function onEvent(e) {
    // e: EventId, Level, MachineName, AgentName, Message
    var machineName = e.MachineName;
    var agentName = e.AgentName;
    var agent = ensureAgent(machineName, agentName);
    setAgentMode(agent, "work");
    if (e.Level > 1)
        agent.addClass(eventLevels[e.Level].toLowerCase());
    agent.children('.info').html(eventTypes[e.EventId]);

    var log = ensureLog(agentName);
    var now = new Date();
    log.lastTime = now;
    e.loggedAt = now;
    // e: EventId, Level, MachineName, AgentName, Message, loggedAt
    checkLogArray();
    log.events.push(e);
    if (log.events.length > maxLogLength) {
        log.events.splice(0, 1);
    }
    setStatesByLog(agent, log);
}
function ensureLog(agentName) {
    var log = logs[agentName];
    if (!log) {
        logs[agentName] = { lastTime: new Date(), events: [] };
        log = logs[agentName];
    }
    return log;
}
function setStatesByLog(agent, log) {
    var maxLevel = 0;
    for (var i = 0; i < log.events.length; i++) {
        var e = log.events[i];
        if (e.Level > maxLevel) {
            maxLevel = e.Level;
        }
    }
    agent.removeClass("information, warning, error");
    agent.addClass(eventLevels[maxLevel].toLowerCase());
}
function viewAgentLog(agent) {

    var agentName = agent.id;
    log = logs[agentName];
    var agentLog = $("#agentLog");

    agentLog.empty();
    var agentLogHtml = '';
    var agentLogTitle = "<b>Agent log </b>" + agentName;
    //agentLog.html("<div><b>Agent log " + agentName + "</b></div>");
    if (!log) {
        agentLogHtml += "__empty__";
        //agentLog.append("__empty__");
    }
    else {
        for (var i = log.events.length - 1; i >= 0; i--) {
            var e = log.events[i];
            agentLogHtml += '<div class="logitem"><div class="logitemHead ' + eventLevels[e.Level].toLowerCase() + 'Level">' + formatDate(e.loggedAt) + ' <strong>' + eventTypes[e.EventId] + '</strong></div><div class="logitemMessage">' + e.Message + '</div></div>';
            //agentLog.append('<div>' + formatDate(e.loggedAt) + ' <span class="' + eventLevels[e.Level].toLowerCase() + 'Level">' + eventTypes[e.EventId] + '</span>: ' + e.Message + '</div>');
        }

        
    }
    overlayManager.showOverlay({
        title: agentLogTitle,
        text: '<div class="inner">' + agentLogHtml + '</div>',
        appendCloseButton: true
    });

    $logitemHead = $('.logitemHead');
    $logitemHead.on('click', function () {
        var that = $(this);
        if (!that.hasClass('open')) {
            that.addClass('open');
            that.next('.logitemMessage').slideDown();
        }
        else {
            that.removeClass('open');
            that.next('.logitemMessage').slideUp();
        }
    });

}
function formatDate(d) {
    return "" + d.getFullYear() + "-" + dd(d.getMonth()) + "-" + dd(d.getDay()) + " " + dd(d.getHours()) + ":" + dd(d.getMinutes()) + ":" + dd(d.getSeconds());
}
function dd(d) {
    if (d > 9)
        return "" + d;
    return "0" + d;
}

//templates


//---------------------------------
function checkConnections() {
    var toDelete = [];
    var now = new Date();

    tempCPUArray = [];
    refreshCpuArray();
    tempRAMArray = [];
    refreshRamArray();

    var cpuSparkline = $(".cpu .cpu-chart").data("kendoSparkline");
    var ramSparkline = $(".ram .ram-chart").data("kendoSparkline");

    if (cpuSparkline !== null) {
        cpuSparkline.refresh();
    }
    if (ramSparkline !== null) {
        ramSparkline.refresh();
    }

    for (var agentName in logs) {
        var log = logs[agentName];
        var lastTime = log.lastTime;
        var diff = now - lastTime;
        var diffMin = (new Date() - lastTime) / (60 * 1000);
        if (diffMin > 2) {
            toDelete.push(agentName);
        }
        else if (diffMin > 1) {
            agent = $("#" + agentName);
            setAgentMode(agent, SN.Resources.BackgroundOperations['Disconnected']);
        }
    }
    for (var i = 0; i < toDelete.length; i++) {
        $("#" + agentName).remove();
        delete logs[agentName];
    }

}

function refreshCpuArray() {
    for (var i = 0; i < cpuArray.length; i++) {
        if (i != 0) {
            tempCPUArray.push(cpuArray[i]);
        }
    }
    cpuArray.data = [];
    for (var j = 0; j < tempCPUArray.length; j++) {
        cpuArray[j] = tempCPUArray[j];
    }
}

function refreshRamArray() {
    for (var i = 0; i < ramArray.length; i++) {
        if (i != 0) {
            tempRAMArray.push(ramArray[i]);
        }
    }
    ramArray.data = [];
    for (var j = 0; j < tempRAMArray.length; j++) {
        ramArray[j] = tempRAMArray[j];
    }
}

function checkLogArray() {
    var tempLogArray = [];
    if (typeof log.events !== 'undefined' && log.events.length > 10) {
        for (var i = 1; i < log.events.length; i++) {
            tempLogArray[i - 1] = log.events[i];
        }
        log.events = [];
        for (var j = 0; j < tempLogArray.length; j++) {
            log.events[j] = tempLogArray[j];
        }
    }
}