(() => {
    const track = document.getElementById('featuredDoctors');
    if (!track) return;
    const previous = document.getElementById('featuredPrevious');
    const next = document.getElementById('featuredNext');
    const update = () => {
        previous.parentElement.hidden = !track.querySelector(':scope > a');
        previous.disabled = track.scrollLeft <= 2;
        next.disabled = track.scrollLeft + track.clientWidth >= track.scrollWidth - 2;
    };
    function move(direction) {
        const step = track.firstElementChild?.getBoundingClientRect().width || track.clientWidth;
        track.scrollBy({left:direction * (step + 24), behavior:matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth'});
    }
    previous.onclick = () => move(-1); next.onclick = () => move(1);
    track.addEventListener('scroll', update, {passive:true});
    new ResizeObserver(update).observe(track);
    update();
})();
