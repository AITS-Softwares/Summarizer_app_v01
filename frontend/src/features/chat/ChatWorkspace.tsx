import { ChatComposer } from './components/ChatComposer'
import { ChatMessages } from './components/ChatMessages'
import { SourcesPanel } from '../documents/components/SourcesPanel'
import type { ChatWorkspaceProps } from './types'

export function ChatWorkspace(props: ChatWorkspaceProps) {
  return (
    <div className="flex min-h-0 flex-1">
      <section className="relative flex min-w-0 flex-1 flex-col">
        <div className="min-h-0 flex-1 overflow-y-auto">
          <div className="mx-auto flex min-h-full w-full max-w-[920px] flex-col px-4 pb-52 pt-8 sm:px-8 sm:pt-12">
            <ChatMessages
              current={props.current}
              isLoading={props.isLoading}
              setPrompt={props.setPrompt}
              editMessage={props.editMessage}
            />
          </div>
        </div>
        <ChatComposer
          pendingFiles={props.pendingFiles}
          prompt={props.prompt}
          isLoading={props.isLoading}
          isListening={props.isListening}
          editingMessageId={props.editingMessageId}
          isDragging={props.isDragging}
          setPrompt={props.setPrompt}
          setIsDragging={props.setIsDragging}
          handleDrop={props.handleDrop}
          submitPrompt={props.submitPrompt}
          stopGeneration={props.stopGeneration}
          cancelEdit={props.cancelEdit}
          toggleVoiceInput={props.toggleVoiceInput}
          removePending={props.removePending}
          openFiles={props.openFiles}
          openFolder={props.openFolder}
        />
      </section>
      <SourcesPanel
        open={props.sourcesOpen}
        sources={props.allSources}
        pendingFiles={props.pendingFiles}
        isLoading={props.isLoading}
        onClose={() => props.setSourcesOpen(false)}
        openFiles={props.openFiles}
        removePending={props.removePending}
        deleteDocument={props.deleteDocument}
        reprocessDocument={props.reprocessDocument}
      />
    </div>
  )
}
