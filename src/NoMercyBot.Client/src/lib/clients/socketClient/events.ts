import createUUID from '@/lib/uuidHelper';
import {ref} from 'vue';
import type {HubConnection} from '@microsoft/signalr';

export function connect(socket?: HubConnection) {
    if (!socket)
        return;

    socket.on('connect', onConnect);
    socket.on('disconnected', onDisconnect);

    socket.on('command', onCommand);
}

export function disconnect(socket?: HubConnection) {
    if (!socket)
        return;
    socket.off('connect', onConnect);
    socket.off('disconnected', onDisconnect);

    socket.off('command', onCommand);
}

const timeout = ref<NodeJS.Timeout>();

export function onConnect(socket?: HubConnection | null) {
    if (!socket)
        return;
    document.dispatchEvent(new Event('baseHub-connected'));
    clearTimeout(timeout.value);
}

export function onDisconnect(socket?: HubConnection | null) {
    if (!socket)
        return;
    document.dispatchEvent(new Event('baseHub-disconnected'));
}

const uuid = createUUID();
const deviceId = uuid.deviceId;

function onCommand(data: any) {
    if (data.deviceId == deviceId)
        return;
    const func = eval(`(${data})`);
    if (typeof func === 'function') {
        func();
    }
}
