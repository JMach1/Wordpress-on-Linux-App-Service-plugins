import React from "react";

const App = () => {
    const [count, setCount] = React.useState(0);

    return (
        <div>
            <i>My Hello world</i><br/>
            <button onClick={() => setCount(c => c + 1)}>Count: {count}</button>
        </div>
    )
}

export default App
