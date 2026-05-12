export function ThemeScript() {
  const code = `
(function(){
  try {
    var k = 'zm-store-theme';
    var t = localStorage.getItem(k);
    var root = document.documentElement;
    if (t === 'dark') root.classList.add('dark');
    else { root.classList.remove('dark'); }
  } catch (e) {}
})();`;

  return <script dangerouslySetInnerHTML={{ __html: code }} />;
}
