import { defaultValueCtx, Editor, rootCtx, editorViewCtx, serializerCtx } from '@milkdown/core';
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
let currentSearchQuery = '';

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

// Fuzzy matching algorithm
function fuzzyMatch(query, text) {
    if (!query) return { matches: true, score: 0, highlights: [] };
    
    query = query.toLowerCase();
    text = text.toLowerCase();
    
    let queryIndex = 0;
    let textIndex = 0;
  let score = 0;
    let highlights = [];
    let consecutiveMatches = 0;
    
    while (queryIndex < query.length && textIndex < text.length) {
        if (query[queryIndex] === text[textIndex]) {
            highlights.push(textIndex);
      consecutiveMatches++;
      
            // Bonus for consecutive matches
          score += 10 + (consecutiveMatches * 5);
  
            // Bonus for matching at the start
       if (textIndex === 0) score += 15;
    
            // Bonus for matching after a space or special char
 if (textIndex > 0 && /[\s\-_]/.test(text[textIndex - 1])) {
                score += 12;
  }
     
            queryIndex++;
     } else {
   consecutiveMatches = 0;
   }
        textIndex++;
    }
    
    // All query characters must be matched
    if (queryIndex < query.length) {
 return { matches: false, score: 0, highlights: [] };
    }

    // Penalty for longer text (prefer shorter matches)
    score -= (text.length - query.length) * 0.5;

    return { matches: true, score, highlights };
}

// Filter items with fuzzy matching
function filterItemsWithFuzzy(items, query) {
    if (!query || query.trim() === '') {
        return items.map(item => ({ ...item, score: 0, labelHighlights: [], descHighlights: [] }));
    }
    
    const results = [];
    
    for (const item of items) {
        // Try matching against label, description, and command
    const labelMatch = fuzzyMatch(query, item.label);
   const descMatch = fuzzyMatch(query, item.description);
      const commandMatch = fuzzyMatch(query, item.command);
        
        // If any field matches, include the item
        if (labelMatch.matches || descMatch.matches || commandMatch.matches) {
 // Use the best score
       const bestScore = Math.max(labelMatch.score, descMatch.score, commandMatch.score);
            results.push({
      ...item,
    score: bestScore,
     labelHighlights: labelMatch.highlights,
            descHighlights: descMatch.highlights
      });
        }
    }
    
    // Sort by score (highest first)
    results.sort((a, b) => b.score - a.score);
    
    return results;
}

// Apply highlights to text
function highlightText(text, highlights) {
    if (!highlights || highlights.length === 0) {
        return text;
    }
    
    let result = '';
    for (let i = 0; i < text.length; i++) {
        if (highlights.includes(i)) {
      result += `<mark class="fuzzy-highlight">${text[i]}</mark>`;
        } else {
  result += text[i];
  }
    }
    return result;
}

// Filter slash menu with fuzzy matching
function filterSlashMenu(query) {
    if (!window._allSlashItems) return;
    
    currentSearchQuery = query;
    const filteredItems = filterItemsWithFuzzy(window._allSlashItems, query);
    updateSlashMenuDisplay(filteredItems);
  
  console.log(`? Slash menu filtered with query: "${query}", ${filteredItems.length} results`);
}

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
    
    // Store all items for filtering
  window._allSlashItems = items;
    
    document.body.appendChild(container);
    slashMenu = container;
    selectedSlashIndex = 0;
    
 return container;
}

function updateSlashMenuDisplay(filteredItems) {
    if (!slashMenu) return;
    
    // Clear existing items
    slashMenu.innerHTML = '';
    slashMenuItems = [];
 
    filteredItems.forEach((item, index) => {
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
        // Apply highlights to label
 if (item.labelHighlights && item.labelHighlights.length > 0) {
labelSpan.innerHTML = highlightText(item.label, item.labelHighlights);
    } else {
        labelSpan.textContent = item.label;
        }

        const descSpan = document.createElement('span');
        descSpan.className = 'slash-dropdown-item-description';
        // Apply highlights to description
        if (item.descHighlights && item.descHighlights.length > 0) {
            descSpan.innerHTML = highlightText(item.description, item.descHighlights);
        } else {
       descSpan.textContent = item.description;
        }
        
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
        slashMenu.appendChild(div);
    });
    
    selectedSlashIndex = 0;
}

function showSlashMenu(x, y) {
if (!slashMenu) {
        createSlashMenu();
    }
    
    // Reset search query
    currentSearchQuery = '';
  
    // Show all items initially
    const filteredItems = filterItemsWithFuzzy(window._allSlashItems || [], '');
    updateSlashMenuDisplay(filteredItems);
    
  slashMenu.style.display = 'block';
    slashMenu.style.left = `${x}px`;
    slashMenu.style.top = `${y + 20}px`;
    
    console.log('? Slash menu shown');
}

function hideSlashMenu() {
    if (slashMenu) {
 slashMenu.style.display = 'none';
    }
    console.log('? Slash menu hidden');
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

async function insertElement(command = '') {
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
          const { $from } = state.selection;
 
       // Calculate the end position (current cursor position)
                const endPos = $from.pos;
    
     // Delete from slash to current position
            let tr = state.tr.delete(slashTriggerPos, endPos);
    const node = schema.nodes.image.create({
              src: imageData.src,
        alt: imageData.alt || 'image',
       title: imageData.title || ''
            });
 
      tr = tr.insert(slashTriggerPos, node);
         dispatch(tr);
     });
  } else {
 editorInstance.action((ctx) => {
         const view = ctx.get(editorViewCtx);
    const { state, dispatch } = view;
  const { schema } = state;
        const { $from } = state.selection;
            
     // Calculate the end position (current cursor position)
         const endPos = $from.pos;

       let tr = state.tr;

         // Delete the slash and everything typed after it
           if (slashTriggerPos !== null) {
                    tr = tr.delete(slashTriggerPos, endPos);
   }
   
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
  
                if (node && slashTriggerPos !== null) {
const $pos = tr.doc.resolve(slashTriggerPos);
        const parent = $pos.parent;

    if (parent.type.name === 'paragraph' && parent.content.size === 0) {
       const start = $pos.before();
  const end = $pos.after();
       
          tr = tr.replaceWith(start, end, node);

     if (command !== 'hr') {
    const newPos = start + 1;
          tr = tr.setSelection(state.selection.constructor.near(tr.doc.resolve(newPos)));
            }
               } else {
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
 ctx.set(defaultValueCtx, ''); // Start with empty content
         
   ctx.get(listenerCtx).markdownUpdated((ctx, markdown) => {
      if (window.chrome && window.chrome.webview) {
         // Get the actual rendered text content instead of markdown
         const view = ctx.get(editorViewCtx);
         const serializer = ctx.get(serializerCtx);
         
         // Get markdown for saving, but plain text for stats
         const markdownContent = serializer(view.state.doc);
         
         // Extract pure text content by removing markdown formatting
         let plainText = markdownContent
            // Remove headings markers
            .replace(/^#+\s+/gm, '')
            // Remove bold/italic markers
            .replace(/\*\*([^*]+)\*\*/g, '$1')
            .replace(/\*([^*]+)\*/g, '$1')
            .replace(/__([^_]+)__/g, '$1')
            .replace(/_([^_]+)_/g, '$1')
            // Remove strikethrough
            .replace(/~~([^~]+)~~/g, '$1')
            // Remove links, keep text
            .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
            // Remove images
            .replace(/!\[([^\]]*)\]\([^)]+\)/g, '')
            // Remove code blocks markers
            .replace(/```[^`]*```/g, '')
            .replace(/`([^`]+)`/g, '$1')
            // Remove blockquote markers
            .replace(/^>\s+/gm, '')
            // Remove list markers
            .replace(/^[\*\-\+]\s+/gm, '')
            .replace(/^\d+\.\s+/gm, '')
            // Remove horizontal rules
            .replace(/^---+$/gm, '')
            .replace(/^\*\*\*+$/gm, '')
            // Normalize whitespace
            .trim();
         
         window.chrome.webview.postMessage({
      type: 'change',
                  content: plainText
          });
        }
      
   // Update placeholder visibility
        updatePlaceholder();
            });
  })
     .use(nord)
        .use(commonmark)
            .use(gfm)
   .use(history)
            .use(listener)
            .create();

    console.log('? Milkdown editor created successfully');
  
        createSlashMenu();
  
        setTimeout(() => {
     const prosemirror = document.querySelector('.ProseMirror');
            if (prosemirror) {
            prosemirror.setAttribute('spellcheck', 'true');
         prosemirror.spellcheck = true;
  console.log('? Spellcheck enabled on ProseMirror');
    
      // Auto-focus the editor
  prosemirror.focus();
        console.log('? Editor auto-focused');
  
     // Create and add placeholder
        createPlaceholder();
  
         prosemirror.addEventListener('keyup', (e) => {
          updatePlaceholder();
     
        if (e.key === '/') {
editorInstance.action((ctx) => {
    const view = ctx.get(editorViewCtx);
          const { state } = view;
      const { selection } = state;
        const { $from } = selection;
    
            const text = $from.parent.textContent;
             const charBefore = text[$from.parentOffset - 1];
    
     if (charBefore === '/') {
           slashTriggerPos = $from.pos - 1;
        const coords = view.coordsAtPos($from.pos);
  showSlashMenu(coords.left, coords.top);
      }
         });
       } else if (isSlashMenuVisible()) {
        // Don't close the menu - instead filter it
         // Only close on specific keys
    if (e.key === ' ' || e.key === 'Escape') {
    hideSlashMenu();
 slashTriggerPos = null;
        } else if (e.key !== 'ArrowUp' && 
           e.key !== 'ArrowDown' && 
   e.key !== 'Enter' &&
           e.key !== 'Shift' &&
      e.key !== 'Control' &&
     e.key !== 'Alt' &&
           e.key !== 'Meta') {
         // Update filter based on text after slash
    editorInstance.action((ctx) => {
     const view = ctx.get(editorViewCtx);
          const { state } = view;
         const { selection } = state;
       const { $from } = selection;
          
     if (slashTriggerPos !== null) {
             const text = $from.parent.textContent;
        const slashOffset = slashTriggerPos - $from.start();
   const currentOffset = $from.parentOffset;
             
          if (currentOffset > slashOffset) {
       const searchQuery = text.substring(slashOffset + 1, currentOffset);
          filterSlashMenu(searchQuery);
   } else if (currentOffset <= slashOffset) {
           // Cursor moved before the slash, close menu
  hideSlashMenu();
       slashTriggerPos = null;
               }
    }
        });
        }
          }
       });
    
 // Also update placeholder on clicks and focus
     prosemirror.addEventListener('click', updatePlaceholder);
  prosemirror.addEventListener('focus', updatePlaceholder);
            }
        }, 500);
    } catch (error) {
   console.error('? Milkdown initialization error:', error);
        document.getElementById('app').innerHTML = `<div style="color: red; padding: 20px;">Error loading editor: ${error.message}</div>`;
    }
}

function createPlaceholder() {
    const existing = document.getElementById('editor-placeholder');
    if (existing) existing.remove();
    
    const placeholder = document.createElement('div');
    placeholder.id = 'editor-placeholder';
    placeholder.textContent = 'Type your markdown here...';
    placeholder.style.cssText = `
        position: absolute;
        top: 36px;
        left: 10%;
        color: #6c757d;
    opacity: 0.5;
  pointer-events: none;
 font-size: clamp(15px, 1.2vw, 20px);
        line-height: 1.7;
        font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    `;
    
    const app = document.getElementById('app');
    if (app) {
        app.appendChild(placeholder);
        updatePlaceholder();
    }
}

function updatePlaceholder() {
    const placeholder = document.getElementById('editor-placeholder');
    if (!placeholder) return;
    
    try {
        if (!editorInstance) {
        placeholder.style.display = 'block';
            return;
        }
        
        const markdown = editorInstance.action((ctx) => {
         const view = ctx.get(editorViewCtx);
         const serializer = ctx.get(serializerCtx);
   return serializer(view.state.doc);
        });
        
   // Hide placeholder if there's any content at all (including spaces and newlines)
        // Don't trim - we want even whitespace to hide the placeholder
        if (markdown && markdown.length > 0) {
 placeholder.style.display = 'none';
        } else {
       placeholder.style.display = 'block';
    }
    } catch (e) {
        console.error('Error updating placeholder:', e);
    }
}

// Set up content change detection
let isTrackingChanges = false;
let changeTimeout;

function setupContentChangeTracking() {
    if (isTrackingChanges) return;
    
    console.log('Setting up content change tracking');
    
    // Listen for input events on the editor
    document.addEventListener('input', () => {
        clearTimeout(changeTimeout);
   changeTimeout = setTimeout(() => {
            console.log('Content changed, notifying C#');
      if (window.chrome && window.chrome.webview) {
 window.chrome.webview.postMessage({ action: 'contentChanged' });
            }
        }, 100); // Reduced from 500ms to 100ms for faster response
    }, true);
    
    // Also listen for keydown events for immediate feedback on certain keys
 document.addEventListener('keydown', (e) => {
        // For printable characters and common editing keys, notify immediately
        if (e.key.length === 1 || ['Backspace', 'Delete', 'Enter', 'Tab'].includes(e.key)) {
            if (window.chrome && window.chrome.webview) {
     window.chrome.webview.postMessage({ action: 'contentChanged' });
         }
        }
    }, true);
    
    // Listen for paste events - immediate notification
    document.addEventListener('paste', () => {
   if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage({ action: 'contentChanged' });
 }
    }, true);
    
    // Listen for cut events - immediate notification  
    document.addEventListener('cut', () => {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'contentChanged' });
 }
    }, true);
    
    isTrackingChanges = true;
    console.log('Content change tracking enabled with fast response');
}

// Call setup after editor is ready
window.setupContentChangeTracking = setupContentChangeTracking;

createEditor();

window.getMarkdown = () => {
    if (!editorInstance) {
        console.warn('getMarkdown called but editor not ready');
        return '';
    }
    try {
     const markdown = editorInstance.action((ctx) => {
         const view = ctx.get(editorViewCtx);
  const serializer = ctx.get(serializerCtx);
    return serializer(view.state.doc);
        });
        
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
    if (isSlashMenuVisible()) {
        // Handle Backspace when menu is open
        if (e.key === 'Backspace') {
  // Check if we're about to delete the slash
    if (editorInstance) {
editorInstance.action((ctx) => {
    const view = ctx.get(editorViewCtx);
         const { state } = view;
    const { selection } = state;
const { $from } = selection;
    
             if (slashTriggerPos !== null) {
       const text = $from.parent.textContent;
    const slashOffset = slashTriggerPos - $from.start();
        const currentOffset = $from.parentOffset;
          
 // If cursor is right after the slash, close menu
             if (currentOffset === slashOffset + 1) {
      hideSlashMenu();
  slashTriggerPos = null;
           }
     }
           });
         }
          return; // Don't prevent default, allow backspace to work
        }

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
    if (e.key === 'Escape' || e.key === ' ') {
         e.preventDefault();
e.stopPropagation();
       hideSlashMenu();
     slashTriggerPos = null;
   return false;
        }
    }
    
    if (e.ctrlKey && e.key === 'n') {
        e.preventDefault();
        e.stopPropagation();
        console.log('Ctrl+N detected, sending to C#');
        if (window.chrome && window.chrome.webview) {
         window.chrome.webview.postMessage({ action: 'new' });
        }
        return false;
    }
    
    if (e.ctrlKey && e.key === 'o') {
        e.preventDefault();
        e.stopPropagation();
      console.log('Ctrl+O detected, sending to C#');
if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ action: 'open' });
    }
        return false;
    }
    
 if (e.ctrlKey && e.shiftKey && e.key === 'S') {
        e.preventDefault();
        e.stopPropagation();
        console.log('Ctrl+Shift+S detected, sending to C#');
    if (window.chrome && window.chrome.webview) {
          window.chrome.webview.postMessage({ action: 'saveAs' });
        }
        return false;
    }
    
    if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        e.stopPropagation();
        console.log('Ctrl+S detected, sending to C#');
        if (window.chrome && window.chrome.webview) {
 window.chrome.webview.postMessage({ action: 'save' });
        }
return false;
    }
    
    if (e.key === 'F2') {
        e.preventDefault();
    e.stopPropagation();
        console.log('F2 detected, sending to C#');
        if (window.chrome && window.chrome.webview) {
 window.chrome.webview.postMessage({ action: 'rename' });
     }
 return false;
  }

    if (e.ctrlKey && e.shiftKey && e.key === 'P') {
      e.preventDefault();
        e.stopPropagation();
        console.log('Ctrl+Shift+P detected, sending to C#');
        if (window.chrome && window.chrome.webview) {
         window.chrome.webview.postMessage({ action: 'commandPalette' });
        }
        return false;
    }
}, true);

document.addEventListener('click', (e) => {
    if (isSlashMenuVisible() && !slashMenu.contains(e.target)) {
        hideSlashMenu();
   slashTriggerPos = null;
    }
});

console.log('? Keyboard shortcuts handler installed (Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+Shift+S, F2, Ctrl+Shift+P, Slash menu navigation)');
