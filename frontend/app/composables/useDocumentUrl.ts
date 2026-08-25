export type DocumentUrlState = 'loading' | 'ready' | 'error'

/** Fetch-on-demand presigned playback/view URL, shared by any component that needs to render a
 * document's bytes directly (video player, image viewer) rather than just downloading them. */
export function useDocumentUrl(documentId: string) {
  const { request } = useApi()

  const state = ref<DocumentUrlState>('loading')
  const url = ref<string | null>(null)


  async function load() {
    state.value = 'loading'
    try {
      const result = await request<{ url: string }>(`/documents/${documentId}/download-url`)
      url.value = result.url
      state.value = 'ready'
    }
    catch {
      state.value = 'error'
    }
  }

  return { state, url, load }
}
