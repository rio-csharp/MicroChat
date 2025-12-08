// 代码块复制功能
export function initializeCodeBlocks(element) {
    if (!element) return;

    // 查找所有代码块：标准格式的 pre 和 ColorCode 格式的 pre
    const preElements = element.querySelectorAll('pre');
    
    preElements.forEach((pre) => {
        // 避免重复添加
        if (pre.querySelector('.code-block-header')) {
            return;
        }

        // 检查是否包含代码行结构
        const hasCodeLines = pre.querySelector('.code-line');
        if (!hasCodeLines) {
            return; // 不是我们处理的代码块
        }

        // 创建代码块头部容器
        const header = document.createElement('div');
        header.className = 'code-block-header';

        // 获取语言信息 - 支持多种格式
        let language = 'text';
        
        // 从 data-language 属性获取
        if (pre.dataset.language) {
            language = pre.dataset.language;
        } else {
            // 从 code 元素的 class 获取
            const codeBlock = pre.querySelector('code');
            if (codeBlock) {
                const languageClass = Array.from(codeBlock.classList).find(cls => 
                    cls.startsWith('language-') || cls.startsWith('lang-')
                );
                
                if (languageClass) {
                    language = languageClass.replace(/^(language-|lang-)/, '');
                } else if (codeBlock.className) {
                    const className = codeBlock.className.trim();
                    if (className && !className.includes(' ')) {
                        language = className;
                    }
                }
            }
        }
        
        // 创建语言标签
        const languageLabel = document.createElement('span');
        languageLabel.className = 'code-language';
        languageLabel.textContent = language;

        // 创建复制按钮
        const copyButton = document.createElement('button');
        copyButton.className = 'copy-button';
        copyButton.innerHTML = `
            <svg class="copy-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M4 4V11H11V4H4ZM3 2H12V12H3V2Z" fill="currentColor"/>
                <path d="M5 14V13H13V5H14V15H5V14Z" fill="currentColor"/>
            </svg>
            <span class="copy-text">复制</span>
            <svg class="check-icon" width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg" style="display: none;">
                <path d="M13.5 3L6 10.5L2.5 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
        `;

        copyButton.addEventListener('click', async () => {
            try {
                // 获取代码文本（不包括行号）
                const codeText = getCodeTextWithoutLineNumbers(pre);
                await navigator.clipboard.writeText(codeText);
                
                // 显示成功状态
                const copyIcon = copyButton.querySelector('.copy-icon');
                const checkIcon = copyButton.querySelector('.check-icon');
                const copyText = copyButton.querySelector('.copy-text');
                
                copyIcon.style.display = 'none';
                checkIcon.style.display = 'inline';
                copyText.textContent = '已复制';
                copyButton.classList.add('copied');
                
                // 2秒后恢复
                setTimeout(() => {
                    copyIcon.style.display = 'inline';
                    checkIcon.style.display = 'none';
                    copyText.textContent = '复制';
                    copyButton.classList.remove('copied');
                }, 2000);
            } catch (err) {
                console.error('Failed to copy code:', err);
            }
        });

        header.appendChild(languageLabel);
        header.appendChild(copyButton);
        
        // 将头部插入到 pre 元素开头
        pre.insertBefore(header, pre.firstChild);
        pre.classList.add('has-header');
    });
}

// 获取代码文本，排除行号
function getCodeTextWithoutLineNumbers(preElement) {
    // 提取所有代码行的内容（不包括行号）
    const lines = preElement.querySelectorAll('.code-line');
    if (lines.length > 0) {
        return Array.from(lines)
            .map(line => {
                const codeContent = line.querySelector('.code-content');
                return codeContent ? codeContent.textContent : '';
            })
            .join('\n');
    }
    
    // 否则直接返回整个 pre 元素的文本
    return preElement.textContent;
}

// 清理代码块（移除动态添加的元素）
export function cleanupCodeBlocks(element) {
    if (!element) return;
    
    const headers = element.querySelectorAll('.code-block-header');
    headers.forEach(header => header.remove());
    
    const pres = element.querySelectorAll('pre.has-header');
    pres.forEach(pre => pre.classList.remove('has-header'));
}
