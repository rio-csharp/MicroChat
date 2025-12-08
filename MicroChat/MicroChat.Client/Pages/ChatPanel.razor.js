// 初始化
export function init(dotNetHelper) {
    const container = document.getElementById('chat-messages-container');
    if (!container) return;

    let scrollTimeout;

    // 检查滚动位置并更新按钮显示状态
    const checkScrollPosition = () => {
        const { scrollTop, scrollHeight, clientHeight } = container;
        const isAtBottom = scrollHeight - scrollTop - clientHeight < 50;
        const isAtTop = scrollTop < 50;
        
        dotNetHelper.invokeMethodAsync('UpdateButtonState', isAtBottom, isAtTop);
    };

    // 监听滚动事件
    container.addEventListener('scroll', () => {
        clearTimeout(scrollTimeout);
        scrollTimeout = setTimeout(checkScrollPosition, 100);
    });
}

// 滚动到底部
export function scrollToBottom() {
    const container = document.getElementById('chat-messages-container');
    if (!container) return;
    
    container.scrollTop = container.scrollHeight;
}

// 滚动到顶部
export function scrollToTop() {
    const container = document.getElementById('chat-messages-container');
    if (!container) return;
    
    container.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
}
