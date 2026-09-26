import { useEventListener } from '@vueuse/core'

const OVERLAY_SELECTOR = '[role="dialog"], [role="alertdialog"], [role="menu"], [role="listbox"]'

function isEditableTarget(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) {
    return false
  }
  return target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)
}

export function useKeyboardShortcuts(shortcuts: Record<string, () => void>) {
  useEventListener(document, 'keydown', (event: KeyboardEvent) => {
    if (event.defaultPrevented || event.ctrlKey || event.metaKey || event.altKey) {
      return
    }
    if (isEditableTarget(event.target) || document.querySelector(OVERLAY_SELECTOR)) {
      return
    }

    const handler = shortcuts[event.key.toLowerCase()]
    if (handler) {
      event.preventDefault()
      handler()
    }
  })
}
