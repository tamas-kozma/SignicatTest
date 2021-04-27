<template>
    <v-app>
        <v-app-bar app dark
                   color="primary">
            <v-spacer></v-spacer>
            <v-btn text
                   @click="onLoginClick">Login</v-btn>
        </v-app-bar>
        <v-main>
        </v-main>
        <v-dialog v-model="isLoginDialogOpen"
                  max-width="60vw"
                  persistent no-click-animation>
            <iframe src="https://localhost:5001/api/session/login"
                    style="height: 60vh;"
                    frameborder="0">
            </iframe>
        </v-dialog>
    </v-app>
</template>

<script lang="ts">
    import { Component, Watch, Vue } from 'vue-property-decorator';

    @Component({ components: { } })
    export default class App extends Vue {
        isLoginDialogOpen: boolean = false;

        onLoginClick(): void {
            this.isLoginDialogOpen = true;
        }
    }
</script>