// Reference-counted body-scroll lock, shared by every overlay that needs one (FormModal,
// AdminModal, EntityPicker's mobile fullscreen sheet). A modal and a picker nested inside it can
// both be "open" at once; a naive `overflow = ''` on the inner one's close would re-enable
// background scroll while the outer modal is still up. Counting instead of set/reset fixes that.
let lockCount = 0

export function lockBodyScroll() {
  lockCount++
  document.body.style.overflow = 'hidden'
}

export function unlockBodyScroll() {
  lockCount = Math.max(0, lockCount - 1)
  if (lockCount === 0) document.body.style.overflow = ''
}
