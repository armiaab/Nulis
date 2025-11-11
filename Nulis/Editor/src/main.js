import { defaultValueCtx, Editor, rootCtx, editorViewCtx } from '@milkdown/core';
import { commonmark } from '@milkdown/preset-commonmark';
import { gfm } from '@milkdown/preset-gfm';
import { nord } from '@milkdown/theme-nord';
import { listener, listenerCtx } from '@milkdown/plugin-listener';
import { history } from '@milkdown/plugin-history';
import { replaceAll } from '@milkdown/utils';

import '@milkdown/theme-nord/style.css';

let editorInstance;
let slashMenu = null;
let slashMenuItems = [];
let selectedSlashIndex = 0;
let slashTriggerPos = null;

// SVG Icons for slash menu
const slashIcons = {
    h1: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
      <path d="M4 12h8M4 18V6M12 18V6M17 12h3M17 18V6"/>
    </svg>`,
    h2: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M4 12h8M4 18V6M12 18V6M17 12h3M17 18v-5a2 2 0 012-2 2 2 0 012 2v5"/>
    </svg>`,
    h3: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M4 12h8M4 18V6M12 18V6M17 10a2 2 0 012-2 2 2 0 012 2v1a2 2 0 01-2 2 2 2 0 012 2v1a2 2 0 01-2 2 2 2 0 01-2-2"/>
    </svg>`,
    bulletList: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
   <line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/>
   <circle cx="3" cy="6" r="1" fill="currentColor"/><circle cx="3" cy="12" r="1" fill="currentColor"/><circle cx="3" cy="18" r="1" fill="currentColor"/>
    </svg>`,
    orderedList: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <line x1="10" y1="6" x2="21" y2="6"/><line x1="10" y1="12" x2="21" y2="12"/><line x1="10" y1="18" x2="21" y2="18"/>
        <path d="M3 5h2v4M3 13h2v4M3 17h4"/>
    </svg>`,
    blockquote: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
    <path d="M3 21c3 0 7-1 7-8V5c0-1.25-.756-2.017-2-2H4c-1.25 0-2 .75-2 1.972V11c0 1.25.75 2 2 2 1 0 1 0 1 1v1c0 1-1 2-2 2s-1 .008-1 1.031V20c0 1 0 1 1 1z"/>
        <path d="M15 21c3 0 7-1 7-8V5c0-1.25-.757-2.017-2-2h-4c-1.25 0-2 .75-2 1.972V11c0 1.25.75 2 2 2h.75c0 2.25.25 4-2.75 4v3c0 1 0 1 1 1z"/>
    </svg>`,
    codeBlock: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>
    </svg>`,
    hr: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
<line x1="3" y1="12" x2="21" y2="12"/>
    </svg>`,
    table: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <rect x="3" y="3" width="18" height="18" rx="2"/><line x1="3" y1="9" x2="21" y2="9"/>
        <line x1="3" y1="15" x2="21" y2="15"/><line x1="12" y1="3" x2="12" y2="21"/>
    </svg>`,
    image: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
     <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/>
        <polyline points="21 15 16 10 5 21"/>
    </svg>`
};

// Create slash menu
function createSlashMenu() {
    if (slashMenu) {
        slashMenu.remove();
    }

    const container = document.createElement('div');
    container.className = 'slash-dropdown';
    container.style.display = 'none';
    container.style.position = 'fixed';
    
    const items = [
        { label: 'Heading 1', command: 'h1', description: 'Large heading' },
        { label: 'Heading 2', command: 'h2', description: 'Medium heading' },
 { label: 'Heading 3', command: 'h3', description: 'Small heading' },
        { label: 'Bullet List', command: 'bulletList', description: 'Unordered list' },
        { label: 'Ordered List', command: 'orderedList', description: 'Numbered list' },
        { label: 'Blockquote', command: 'blockquote', description: 'Quote text' },
        { label: 'Code Block', command: 'codeBlock', description: 'Code snippet' },
        { label: 'Horizontal Rule', command: 'hr', description: 'Divider line' },
     { label: 'Table', command: 'table', description: 'Insert table' },
        { label: 'Image', command: 'image', description: 'Insert image' }
    ];
    
    slashMenuItems = [];
    items.forEach((item, index) => {
        const div = document.createElement('div');
      div.className = 'slash-dropdown-item';
        if (index === 0) div.classList.add('selected');
      
        const iconSpan = document.createElement('span');
        iconSpan.className = 'slash-dropdown-item-icon';
        iconSpan.innerHTML = slashIcons[item.command];
        
   const contentDiv = document.createElement('div');
        contentDiv.className = 'slash-dropdown-item-content';
        
     const labelSpan = document.createElement('span');
        labelSpan.className = 'slash-dropdown-item-label';
        labelSpan.textContent = item.label;
     
      const descSpan = document.createElement('span');
        descSpan.className = 'slash-dropdown-item-description';
  descSpan.textContent = item.description;
        
        contentDiv.appendChild(labelSpan);
        contentDiv.appendChild(descSpan);
        
        div.appendChild(iconSpan);
    div.appendChild(contentDiv);
        
        div.addEventListener('mousedown', (e) => {
        e.preventDefault();
            e.stopPropagation();
        });
        
        div.addEventListener('click', (e) => {
  e.preventDefault();
   e.stopPropagation();
          insertElement(item.command);
    });
        
    slashMenuItems.push({ element: div, command: item.command });
      container.appendChild(div);
    });
 
    document.body.appendChild(container);
    slashMenu = container;
    selectedSlashIndex = 0;
    
    return container;
}

function showSlashMenu(x, y) {
    if (!slashMenu) {
        createSlashMenu();
    }
    
    slashMenu.style.display = 'block';
    slashMenu.style.left = `${x}px`;
    slashMenu.style.top = `${y + 20}px`;
    
    selectedSlashIndex = 0;
    slashMenuItems.forEach((item, index) => {
        item.element.classList.toggle('selected', index === 0);
    });
    
    console.log('✓ Slash menu shown');
}

function hideSlashMenu() {
    if (slashMenu) {
        slashMenu.style.display = 'none';
    }
    console.log('✓ Slash menu hidden');
}

function isSlashMenuVisible() {
    return slashMenu && slashMenu.style.display === 'block';
}

function updateSlashSelection(newIndex) {
    if (slashMenuItems.length === 0) return;
  
    slashMenuItems[selectedSlashIndex].element.classList.remove('selected');
    
    selectedSlashIndex = newIndex;
    if (selectedSlashIndex < 0) selectedSlashIndex = slashMenuItems.length - 1;
    if (selectedSlashIndex >= slashMenuItems.length) selectedSlashIndex = 0;
    
    slashMenuItems[selectedSlashIndex].element.classList.add('selected');
    slashMenuItems[selectedSlashIndex].element.scrollIntoView({ block: 'nearest' });
}

function executeSelectedSlashItem() {
 if (slashMenuItems.length === 0) return;
 const selectedItem = slashMenuItems[selectedSlashIndex];
    insertElement(selectedItem.command);
}

async function pickImageFile() {
    try {
   if (window.chrome && window.chrome.webview) {
            return new Promise((resolve) => {
        window._imagePickerResolve = resolve;
       window.chrome.webview.postMessage({ action: 'pickImage' });
            });
     }
        return null;
    } catch (error) {
        console.error('Error picking image:', error);
        return null;
    }
}

async function insertElement(command) {
    if (!editorInstance) return;
 
    console.log('Inserting element:', command);
    hideSlashMenu();
    
    try {
        if (command === 'image') {
            const imageData = await pickImageFile();
         if (!imageData) {
    console.log('No image selected');
   return;
   }
        
            editorInstance.action((ctx) => {
  const view = ctx.get(editorViewCtx);
      const { state, dispatch } = view;
    const { schema } = state;
    
    // Delete the "/" and insert image at that position
     let tr = state.tr.delete(slashTriggerPos, slashTriggerPos + 1);
         const node = schema.nodes.image.create({
     src: imageData.src,
        alt: imageData.alt || 'image',
            title: imageData.title || ''
   });
 
    // Insert at the slash position (which is now empty after delete)
tr = tr.insert(slashTriggerPos, node);
        dispatch(tr);
            });
        } else {
       editorInstance.action((ctx) => {
              const view = ctx.get(editorViewCtx);
        const { state, dispatch } = view;
                const { schema } = state;
  
                // Get current position info
     const { $from } = state.selection;
                
       // Start fresh transaction
                let tr = state.tr;
         
 // Delete the "/" character first
    tr = tr.delete(slashTriggerPos, slashTriggerPos + 1);
     
      // Create the node
       let node;
    switch (command) {
        case 'h1':
     case 'h2':
             case 'h3':
    const level = parseInt(command.substring(1));
          node = schema.nodes.heading.create({ level });
             break;
    case 'bulletList':
               const bulletItem = schema.nodes.list_item.create(
   null,
         schema.nodes.paragraph.create()
      );
          node = schema.nodes.bullet_list.create(null, bulletItem);
     break;
   case 'orderedList':
  const orderedItem = schema.nodes.list_item.create(
   null,
     schema.nodes.paragraph.create()
    );
              node = schema.nodes.ordered_list.create(null, orderedItem);
  break;
 case 'blockquote':
         const quotePara = schema.nodes.paragraph.create();
    node = schema.nodes.blockquote.create(null, quotePara);
     break;
     case 'codeBlock':
 node = schema.nodes.code_block.create();
           break;
        case 'hr':
         node = schema.nodes.hr.create();
 break;
              case 'table':
            if (schema.nodes.table) {
    const cell1 = schema.nodes.table_cell.create(
    null,
             schema.nodes.paragraph.create()
                  );
     const cell2 = schema.nodes.table_cell.create(
        null,
         schema.nodes.paragraph.create()
      );
 const row = schema.nodes.table_row.create(null, [cell1, cell2]);
       node = schema.nodes.table.create(null, row);
     }
     break;
       }
        
         if (node) {
         // Get the position in the updated doc (after deleting /)
 const $pos = tr.doc.resolve(slashTriggerPos);
      const parent = $pos.parent;
        
        // If we're in an empty paragraph, replace it entirely
     if (parent.type.name === 'paragraph' && parent.content.size === 0) {
        // Get paragraph boundaries
    const start = $pos.before();
        const end = $pos.after();
         
        // Replace the entire empty paragraph with our new node
           tr = tr.replaceWith(start, end, node);
       
  // Position cursor inside new node (except hr)
   if (command !== 'hr') {
      const newPos = start + 1;
      tr = tr.setSelection(state.selection.constructor.near(tr.doc.resolve(newPos)));
          }
       } else {
 // Insert inline or at current position
             tr = tr.insert(slashTriggerPos, node);
           
  if (command !== 'hr') {
        const newPos = slashTriggerPos + 1;
        tr = tr.setSelection(state.selection.constructor.near(tr.doc.resolve(newPos)));
            }
   }
     }
      
    dispatch(tr);
      view.focus();
  });
        }
    } catch (error) {
        console.error('Error inserting element:', error);
    }
}

window.onImagePicked = (imageDataUrl, fileName) => {
    if (window._imagePickerResolve) {
        window._imagePickerResolve({
       src: imageDataUrl,
            alt: fileName,
            title: fileName
     });
      delete window._imagePickerResolve;
    }
};

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

        console.log('✓ Milkdown editor created successfully');
        
        // Create slash menu
        createSlashMenu();
        
        // Set up input handler for slash command
        setTimeout(() => {
            const prosemirror = document.querySelector('.ProseMirror');
        if (prosemirror) {
  prosemirror.setAttribute('spellcheck', 'true');
     prosemirror.spellcheck = true;
     console.log('✓ Spellcheck enabled on ProseMirror');
    
          // Add keyup listener for slash - trigger menu immediately
                prosemirror.addEventListener('keyup', (e) => {
  if (e.key === '/') {
          editorInstance.action((ctx) => {
  const view = ctx.get(editorViewCtx);
            const { state } = view;
     const { selection } = state;
      const { $from } = selection;
          
      // Check if the last character is /
   const text = $from.parent.textContent;
              const charBefore = text[$from.parentOffset - 1];
     
          if (charBefore === '/') {
      slashTriggerPos = $from.pos - 1;
 const coords = view.coordsAtPos($from.pos);
    showSlashMenu(coords.left, coords.top);
       }
       });
    } else if (isSlashMenuVisible() && 
          e.key !== 'ArrowUp' && 
              e.key !== 'ArrowDown' && 
             e.key !== 'Enter' && 
  e.key !== 'Escape' &&
      e.key !== 'Shift' &&
e.key !== 'Control' &&
         e.key !== 'Alt') {
       // User typed a regular character - close menu but keep the /
    hideSlashMenu();
       slashTriggerPos = null;
      }
      });
  }
     }, 500);
    } catch (error) {
        console.error('✗ Milkdown initialization error:', error);
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
        console.log('✓ Markdown content updated successfully');
    } catch (e) {
   console.error('✗ setMarkdown error:', e);
        
   try {
  editorInstance.action((ctx) => {
       const view = ctx.get(editorViewCtx);
      const state = view.state;
     const tr = state.tr.replaceWith(0, state.doc.content.size, 
  state.schema.text(text || ''));
      view.dispatch(tr);
         console.log('✓ Markdown updated via fallback method');
        });
        } catch (e2) {
  console.error('✗ Fallback setMarkdown also failed:', e2);
        }
    }
};

window.setTheme = (isDark) => {
    document.body.classList.toggle('light', !isDark);
};

document.addEventListener('keydown', (e) => {
    if (isSlashMenuVisible()) {
        if (e.key === 'ArrowDown') {
        e.preventDefault();
    e.stopPropagation();
   updateSlashSelection(selectedSlashIndex + 1);
  return false;
        }
if (e.key === 'ArrowUp') {
            e.preventDefault();
 e.stopPropagation();
    updateSlashSelection(selectedSlashIndex - 1);
    return false;
  }
   if (e.key === 'Enter') {
      e.preventDefault();
     e.stopPropagation();
 executeSelectedSlashItem();
   return false;
        }
    if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();
            hideSlashMenu();
            slashTriggerPos = null;
  return false;
}
    }
    
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

// Click outside to close
document.addEventListener('click', (e) => {
    if (isSlashMenuVisible() && !slashMenu.contains(e.target)) {
        hideSlashMenu();
      slashTriggerPos = null;
    }
});

console.log('✓ Keyboard shortcuts handler installed (Ctrl+O, Ctrl+R, Slash menu navigation)');
