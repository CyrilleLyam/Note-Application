<script setup lang="ts">
import type { AppLocale } from '@/plugins/i18n'
import { Languages, LogIn, LogOut, Moon, NotebookPen, Sun, User, UserCog } from '@lucide/vue'
import { useDark, useToggle } from '@vueuse/core'
import { storeToRefs } from 'pinia'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRouter } from 'vue-router'
import { Button } from '@/core/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/core/components/ui/dropdown-menu'
import { useSettingsStore } from '@/core/store'
import { useAuthStore } from '@/modules/auth'
import ProfileDialog from '@/modules/auth/components/ProfileDialog.vue'
import { NotificationBell } from '@/modules/notifications'
import { isSupportedLocale, LOCALE_LABELS, SUPPORTED_LOCALES } from '@/plugins/i18n'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const settings = useSettingsStore()
const { isAuthenticated, user, isLoggingOut } = storeToRefs(auth)
const { locale } = storeToRefs(settings)

const isDark = useDark()
const toggleDark = useToggle(isDark)

const initials = computed(() => user.value?.username.slice(0, 2).toUpperCase() ?? '')
const showProfileDialog = ref(false)

onMounted(() => {
  if (isAuthenticated.value && !user.value) {
    auth.fetchCurrentUser()
  }
})

function changeLocale(value: unknown) {
  if (isSupportedLocale(value)) {
    settings.setLocale(value as AppLocale)
  }
}

async function handleLogout() {
  await auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <header class="sticky top-0 z-40 w-full border-b border-border/40 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
    <div class="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
      <RouterLink to="/" class="flex items-center gap-2 font-bold text-xl tracking-tight">
        <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground shadow-sm">
          <NotebookPen class="h-5 w-5" />
        </div>
        <span class="hidden sm:inline">{{ t('nav.brand') }}</span>
      </RouterLink>

      <div class="flex items-center gap-1 sm:gap-2">
        <DropdownMenu>
          <DropdownMenuTrigger as-child>
            <Button variant="ghost" size="icon" class="rounded-full" :aria-label="t('nav.language')">
              <Languages class="h-5 w-5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" class="w-40">
            <DropdownMenuLabel>{{ t('nav.language') }}</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuRadioGroup :model-value="locale" @update:model-value="changeLocale">
              <DropdownMenuRadioItem v-for="code in SUPPORTED_LOCALES" :key="code" :value="code" class="cursor-pointer" :lang="code">
                {{ LOCALE_LABELS[code] }}
              </DropdownMenuRadioItem>
            </DropdownMenuRadioGroup>
          </DropdownMenuContent>
        </DropdownMenu>

        <Button
          variant="ghost"
          size="icon"
          class="rounded-full"
          :aria-label="isDark ? t('nav.switchToLight') : t('nav.switchToDark')"
          @click="toggleDark()"
        >
          <Sun v-if="isDark" class="h-5 w-5" />
          <Moon v-else class="h-5 w-5" />
        </Button>

        <template v-if="isAuthenticated">
          <NotificationBell />
          <DropdownMenu>
            <DropdownMenuTrigger as-child>
              <Button variant="ghost" class="h-10 gap-2 rounded-full px-1.5 sm:pr-3" :aria-label="t('nav.accountMenu')">
                <span class="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary overflow-hidden">
                  <img
                    v-if="user?.avatarUrl"
                    :src="user.avatarUrl"
                    alt="Avatar"
                    class="h-full w-full object-cover"
                  >
                  <template v-else-if="initials">{{ initials }}</template>
                  <User v-else class="h-4 w-4" />
                </span>
                <span class="hidden sm:inline max-w-32 truncate text-sm font-medium">
                  {{ user?.displayName || user?.username }}
                </span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" class="w-56">
              <DropdownMenuLabel class="flex flex-col gap-0.5">
                <span class="truncate">{{ user?.displayName || user?.username || t('nav.myAccount') }}</span>
                <span v-if="user" class="truncate text-xs font-normal text-muted-foreground">
                  {{ user.email }}
                </span>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                class="cursor-pointer"
                @select="showProfileDialog = true"
              >
                <UserCog class="h-4 w-4" />
                <span>Profile Settings</span>
              </DropdownMenuItem>
              <DropdownMenuItem
                variant="destructive"
                class="cursor-pointer"
                :disabled="isLoggingOut"
                @select="handleLogout"
              >
                <LogOut class="h-4 w-4" />
                <span>{{ t('nav.logOut') }}</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </template>
        <template v-else>
          <Button variant="ghost" size="sm" as-child>
            <RouterLink to="/login">
              {{ t('nav.signIn') }}
            </RouterLink>
          </Button>
          <Button size="sm" class="gap-2" as-child>
            <RouterLink to="/register">
              <LogIn class="hidden h-4 w-4 sm:block" />
              <span>{{ t('nav.getStarted') }}</span>
            </RouterLink>
          </Button>
        </template>
      </div>
    </div>
  </header>

  <ProfileDialog v-model:open="showProfileDialog" />
</template>
