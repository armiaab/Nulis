import { defaultValueCtx, Editor, rootCtx, editorViewCtx } from '@milkdown/core';
import { commonmark } from '@milkdown/preset-commonmark';
import { gfm } from '@milkdown/preset-gfm';
import { nord } from '@milkdown/theme-nord';
import { listener, listenerCtx } from '@milkdown/plugin-listener';
import { history } from '@milkdown/plugin-history';
import { replaceAll } from '@milkdown/utils';

import '@milkdown/theme-nord/style.css';

let editorInstance;

async function createEditor() {
    try {
        editorInstance = await Editor.make()
            .config((ctx) => {
                ctx.set(rootCtx, document.getElementById('app'));
                ctx.set(defaultValueCtx, '# Start writing...\n\nType your markdown here.');
                
                ctx.get(listenerCtx).markdownUpdated((ctx, markdown) => {
                    if (window.chrome && window.chrome.webview) {
                        window.chrome.webview.postMessage({
                            type: 'change',
                            content: markdown
                        });
                    }
                });
            })
            .use(nord)
            .use(commonmark)
            .use(gfm)
            .use(history)
            .use(listener)
            .create();

        console.log('? Milkdown editor created successfully');
        
        setTimeout(() => {
            const prosemirror = document.querySelector('.ProseMirror');
            if (prosemirror) {
                prosemirror.setAttribute('spellcheck', 'true');
                prosemirror.spellcheck = true;
                console.log('? Spellcheck enabled on ProseMirror');
            }
        }, 500);
    } catch (error) {
        console.error('? Milkdown initialization error:', error);
        document.getElementById('app').innerHTML = `<div style="color: red; padding: 20px;">Error loading editor: ${error.message}</div>`;
    }
}

createEditor();

window.getMarkdown = () => {
    if (!editorInstance) {
        console.warn('getMarkdown called but editor not ready');
        return '';
    }
    try {
        const view = editorInstance.action((ctx) => {
            return ctx.get(editorViewCtx);
        });
        
        const markdown = view.state.doc.textContent || '';
        console.log('getMarkdown returning:', markdown.substring(0, 100) + '...');
        return markdown;
    } catch (e) {
        console.error('getMarkdown error:', e);
        return '';
    }
};

window.setMarkdown = (text) => {
    if (!editorInstance) {
        console.error('setMarkdown called but editor not ready');
        return;
    }
    
    console.log('setMarkdown called with text length:', text?.length);
    
    try {
        editorInstance.action(replaceAll(text || ''));
        console.log('? Markdown content updated successfully');
    } catch (e) {
        console.error('? setMarkdown error:', e);
        
        try {
            editorInstance.action((ctx) => {
                const view = ctx.get(editorViewCtx);
                const state = view.state;
                const tr = state.tr.replaceWith(0, state.doc.content.size, 
                    state.schema.text(text || ''));
                view.dispatch(tr);
                console.log('? Markdown updated via fallback method');
            });
        } catch (e2) {
            console.error('? Fallback setMarkdown also failed:', e2);
        }
    }
};

window.setTheme = (isDark) => {
    document.body.classList.toggle('light', !isDark);
};

document.addEventListener('keydown', (e) => {
    if (e.ctrlKey && e.key === 'o') {
        e.preventDefault();
        console.log('Ctrl+O detected, sending to C#');
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'open' });
        }
        return false;
    }
    
    if (e.ctrlKey && !e.shiftKey && e.key === 'r') {
        e.preventDefault();
        console.log('Ctrl+R detected, sending to C#');
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'rename' });
        }
        return false;
    }
}, true);

console.log('? Keyboard shortcuts handler installed (Ctrl+O, Ctrl+R)');
