import axios, { AxiosInstance } from 'axios';
import * as BackendDataModel from './BackendDataModel';
import Config from "./Config";

class BackendClient {
    private readonly instance: AxiosInstance;

    constructor(baseUrl: string) {
        this.instance = axios.create({
            baseURL: baseUrl,
            withCredentials: true
        });
    }

    async getUserInfo() {
        let response = await this.instance.get<BackendDataModel.UserInfo>("/session/info");
        return response.data;
    }

    async logOut() {
        await this.instance.post("/session/logout");
    }
}

export default new BackendClient(<string>Config.backendBaseUrl);

