<template>
    <v-app>
        <v-app-bar app dark
                   color="primary">
            <v-spacer></v-spacer>
            <v-btn text
                   @click="onLoginClick"
                   v-if="!isLoading && userInfo == null">Login</v-btn>
            <v-btn text
                   @click="onLogoutClick"
                   v-if="!isLoading && userInfo != null">Logout</v-btn>
        </v-app-bar>
        <v-main>
            <div class="ma-4">
                <span v-if="userInfo != null">
                    Hi, {{userInfo.fullName}}!
                </span>
            </div>
        </v-main>
    </v-app>
</template>

<script lang="ts">
    import { Component, Watch, Vue } from 'vue-property-decorator';
    import * as BackendDataModel from './BackendDataModel';
    import BackendClient from './BackendClient';
    import Config from "./Config";

    @Component({ components: { } })
    export default class App extends Vue {
        isLoading: boolean = false;
        userInfo: BackendDataModel.UserInfo | null = null;

        onLoginClick(): void {
            let backendBaseUrl = <string>Config.backendBaseUrl;
            let loginPageUrl = backendBaseUrl + "session/login";
            window.location.href = loginPageUrl;
        }

        async onLogoutClick() {
            this.userInfo = null;
            await BackendClient.logOut();
        }

        async created() {
            this.isLoading = true;
            try {
                this.userInfo = null;
                this.userInfo = await BackendClient.getUserInfo();
            } catch {
            } finally {
                this.isLoading = false;
            }
        }
    }
</script>